"""
DataScrub ML Servisi

ASP.NET Core backend'inin çağırdığı üç ana endpoint burada tanımlı.
Her endpoint dosyayı okur, ilgili tespit modülünü çalıştırır, JSON döner.
.NET tarafı bu servisin Pandas/RapidFuzz kullandığını bilmez, sadece HTTP sözleşmesini bilir.
"""
import os
import pandas as pd
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional

from duplicate_detection import detect_duplicates
from missing_data import detect_missing_values
from format_fixing import detect_format_errors
from cleaning import apply_cleaning

app = FastAPI(title="DataScrub ML Service", version="1.0.0")


class AnalysisRequest(BaseModel):
    dataset_id: str
    file_path: str


class IssueItem(BaseModel):
    row_index: int
    related_row_index: Optional[int] = None
    column_name: str
    original_value: Optional[str] = None
    suggested_value: Optional[str] = None
    confidence_score: float


class AnalysisResponse(BaseModel):
    issues: List[IssueItem]


class ResolutionItem(BaseModel):
    type: str
    row_index: int
    related_row_index: Optional[int] = None
    column_name: str
    suggested_value: Optional[str] = None


class ApplyCleaningRequest(BaseModel):
    file_path: str
    resolutions: List[ResolutionItem]


class ApplyCleaningResponse(BaseModel):
    cleaned_file_path: str
    rows_before: int
    rows_after: int


def _load_dataframe(file_path: str) -> pd.DataFrame:
    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail=f"Dosya bulunamadı: {file_path}")

    # dtype=str önemli: pandas'ın telefon/posta kodu gibi rakamlardan oluşan metinleri
    # otomatik sayıya çevirip başındaki "0"ı silmesini engelliyoruz. Bu, gerçek veri
    # temizleme araçlarında sık atlanan ama veri kaybına yol açan bir detay.
    if file_path.endswith(".csv"):
        return pd.read_csv(file_path, dtype=str)
    elif file_path.endswith(".xlsx"):
        return pd.read_excel(file_path, dtype=str)
    else:
        raise HTTPException(status_code=400, detail="Desteklenmeyen dosya formatı.")


@app.get("/health")
def health_check():
    return {"status": "ok"}


class InspectResponse(BaseModel):
    row_count: int
    column_count: int


@app.post("/inspect", response_model=InspectResponse)
def inspect_endpoint(request: AnalysisRequest):
    # Yükleme anında satır/kolon sayısını .NET tarafı hesaplamıyor (xlsx okumak için
    # kütüphane gerekir); dosyayı zaten okuyabilen bu servise soruyoruz.
    df = _load_dataframe(request.file_path)
    return InspectResponse(row_count=len(df), column_count=len(df.columns))


@app.post("/detect-duplicates", response_model=AnalysisResponse)
def detect_duplicates_endpoint(request: AnalysisRequest):
    df = _load_dataframe(request.file_path)

    # Mükerrer tespiti için genelde isim/email/telefon gibi kimlik kolonları kullanılır.
    # Önce kolon adına bakarak "kimlik" kolonlarını önceliklendiriyoruz (ad, email, telefon vb.);
    # bulunamazsa ilk 3 metin kolonuna düşüyoruz. Gerçek üründe kullanıcı bunu seçebilir.
    identifier_keywords = ["ad", "isim", "name", "soyad", "email", "e-posta", "telefon", "phone", "gsm"]
    # pandas sürümüne göre metin kolonları "object" ya da kendi "string" dtype'ı olabilir,
    # bu yüzden pd.api.types.is_string_dtype ile sürümden bağımsız kontrol ediyoruz.
    text_columns = [c for c in df.columns if pd.api.types.is_string_dtype(df[c])]
    identifier_columns = [c for c in text_columns if any(k in c.lower() for k in identifier_keywords)]
    text_columns = identifier_columns[:3] if identifier_columns else text_columns[:3]
    if not text_columns:
        return AnalysisResponse(issues=[])

    raw_issues = detect_duplicates(df, match_columns=text_columns)
    return AnalysisResponse(issues=[IssueItem(**i) for i in raw_issues])


def _guess_group_column(df: pd.DataFrame) -> Optional[str]:
    """
    Eksik veri tahmini için hangi kolona göre gruplama yapılacağını tahmin eder.
    Önce isimden (şehir, il, kategori, grup, tip) tanımaya çalışır; bulamazsa
    düşük kardinaliteli (çok tekrar eden değerleri olan) bir metin kolonu seçer -
    yüksek kardinaliteli kolonlar (örn. email) gruplama için anlamsızdır.
    """
    keywords = ["sehir", "il", "city", "kategori", "grup", "tip", "type", "bolge"]
    for col in df.columns:
        if any(k in col.lower() for k in keywords):
            return col

    text_columns = [c for c in df.columns if pd.api.types.is_string_dtype(df[c])]
    best_col, best_ratio = None, 1.0
    for col in text_columns:
        non_null = df[col].dropna()
        if len(non_null) == 0:
            continue
        ratio = non_null.nunique() / len(non_null)  # 1.0 = hepsi benzersiz, 0'a yakın = çok tekrarlı
        if ratio < 0.5 and ratio < best_ratio:
            best_col, best_ratio = col, ratio
    return best_col


@app.post("/detect-missing", response_model=AnalysisResponse)
def detect_missing_endpoint(request: AnalysisRequest):
    df = _load_dataframe(request.file_path)
    group_col = _guess_group_column(df)
    raw_issues = detect_missing_values(df, group_by_column=group_col)
    return AnalysisResponse(issues=[IssueItem(**i) for i in raw_issues])


@app.post("/fix-format", response_model=AnalysisResponse)
def fix_format_endpoint(request: AnalysisRequest):
    df = _load_dataframe(request.file_path)
    raw_issues = detect_format_errors(df)
    return AnalysisResponse(issues=[IssueItem(**i) for i in raw_issues])


@app.post("/apply-cleaning", response_model=ApplyCleaningResponse)
def apply_cleaning_endpoint(request: ApplyCleaningRequest):
    df = _load_dataframe(request.file_path)
    rows_before = len(df)

    resolutions = [r.model_dump() for r in request.resolutions]
    cleaned_df = apply_cleaning(df, resolutions)

    # Temiz dosyayı orijinalin yanına "_cleaned" son ekiyle kaydediyoruz.
    base, ext = os.path.splitext(request.file_path)
    output_path = f"{base}_cleaned{ext}"

    if ext == ".csv":
        cleaned_df.to_csv(output_path, index=False)
    else:
        cleaned_df.to_excel(output_path, index=False)

    return ApplyCleaningResponse(
        cleaned_file_path=output_path,
        rows_before=rows_before,
        rows_after=len(cleaned_df),
    )


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
