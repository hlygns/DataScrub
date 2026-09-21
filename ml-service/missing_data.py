"""
Eksik veri tespiti modülü.

Yaklaşım: Sadece "boş" demekle kalmıyoruz - kategorik kolonlarda en sık görülen değeri,
sayısal kolonlarda medyanı öneriyoruz. Eğer satırda "grup belirleyici" bir kolon varsa
(örn. şehir), o gruptaki değerlerden öneri üretmek daha isabetli (global ortalama yerine).

Önemli: her kolon için tahmin üretmek doğru değil. Telefon, e-posta, isim gibi kişiye özgü
kolonlarda "en sık görülen değer" başka birinin verisini yazmak olur. Bu kolonlar için
öneri üretmiyoruz; eksik hücre kullanıcıya bırakılıyor.
"""
import re
import pandas as pd
from typing import List, Dict, Any, Optional

# Kişiye/kayda özgü değer taşıyan kolonlar: bunlar için "tahmin" anlamsız ve zararlı.
IDENTIFIER_KEYWORDS = [
    "telefon", "phone", "gsm", "tel", "email", "e-posta", "eposta", "mail",
    "tc", "kimlik", "id", "ad", "isim", "name", "soyad", "adres", "address",
    "tarih", "date", "dogum", "iban", "no", "numara", "number",
]

# Kısa anahtar kelimeler ("ad", "id", "tc", "tel", "no") başka kelimelerin içinde
# yanlış eşleşmesin diye (örn. "grad" içinde "ad", "bilgi_notu" içinde "no") alt dize
# yerine kolon adının kelimeleriyle (_ ve - ile bölünmüş) birebir eşleştiriyoruz.
_SHORT_KEYWORDS = {"ad", "id", "tc", "tel", "no"}

# Bu orandan fazla benzersiz değeri olan kolon (her satırı farklı) tahmin için uygun değil.
UNIQUE_RATIO_LIMIT = 0.9
MIN_ROWS_FOR_UNIQUE_CHECK = 5


def _column_tokens(name: str) -> List[str]:
    return [t for t in re.split(r"[^a-z0-9çğıöşü]+", name.lower()) if t]


def _is_identifier_column(name: str) -> bool:
    lowered = name.lower()
    tokens = _column_tokens(name)
    for keyword in IDENTIFIER_KEYWORDS:
        if keyword in _SHORT_KEYWORDS:
            if keyword in tokens:
                return True
        elif keyword in lowered:
            return True
    return False


def _non_empty(series: pd.Series) -> pd.Series:
    """NaN ve sadece boşluktan oluşan hücreleri eler."""
    cleaned = series.dropna()
    return cleaned[cleaned.astype(str).str.strip() != ""]


def _is_mostly_unique(series: pd.Series) -> bool:
    values = _non_empty(series)
    if len(values) < MIN_ROWS_FOR_UNIQUE_CHECK:
        return False
    return values.nunique() / len(values) >= UNIQUE_RATIO_LIMIT


def _is_numeric_column(series: pd.Series) -> bool:
    """
    Dosya dtype=str ile okunduğu için pandas hiçbir kolonu sayısal görmez; bu yüzden
    değerlere bakarak kendimiz karar veriyoruz.
    Baştaki sıfırı olan değerler ("06100", "0532...") sayı değil kimlik/kod sayılır:
    medyanını almak anlamsız ve sıfırı kaybettirir.
    """
    values = _non_empty(series).astype(str).str.strip()
    if values.empty:
        return False
    if values.str.match(r"^0\d").any():
        return False
    return pd.to_numeric(values, errors="coerce").notna().all()


def _to_numeric(series: pd.Series) -> pd.Series:
    return pd.to_numeric(_non_empty(series).astype(str).str.strip(), errors="coerce").dropna()


def _format_number(value: float) -> str:
    # 30.0 yerine 30 yaz: kolonda tam sayılar varsa öneri de tam sayı görünsün
    rounded = round(float(value), 2)
    return str(int(rounded)) if rounded.is_integer() else str(rounded)


def detect_missing_values(
    df: pd.DataFrame,
    group_by_column: Optional[str] = None,
) -> List[Dict[str, Any]]:
    """
    group_by_column: varsa, öneriler bu kolona göre gruplanarak üretilir
    (örn. eksik "şehir" değeri için aynı "posta_kodu" grubundaki en sık şehir önerilir).
    """
    issues: List[Dict[str, Any]] = []

    for column in df.columns:
        missing_mask = df[column].isna() | (df[column].astype(str).str.strip() == "")
        missing_indices = df[missing_mask].index.tolist()
        if not missing_indices:
            continue

        if _is_identifier_column(column):
            continue

        is_numeric = _is_numeric_column(df[column])

        # Her satırı farklı olan METİN kolonlarında (kod, referans no vb.) "en sık değer"
        # anlamsız. Sayısal kolonlarda ise değerlerin hepsi farklı olsa bile (yaş, fiyat)
        # medyan gayet anlamlı, o yüzden bu kontrol sadece metne uygulanıyor.
        if not is_numeric and _is_mostly_unique(df[column]):
            continue

        for idx in missing_indices:
            suggestion, confidence = _suggest_value(df, column, idx, group_by_column, is_numeric)
            if suggestion is None:
                continue

            issues.append({
                "row_index": int(idx),
                "related_row_index": None,
                "column_name": column,
                "original_value": None,
                "suggested_value": str(suggestion),
                "confidence_score": confidence,
            })

    return issues


def _suggest_value(
    df: pd.DataFrame,
    column: str,
    row_idx: int,
    group_by_column: Optional[str],
    is_numeric: bool,
):
    # Gruplama kolonu varsa önce grup içinden öneri üretmeyi dene - daha isabetli
    if group_by_column and group_by_column != column and group_by_column in df.columns \
            and pd.notna(df.at[row_idx, group_by_column]):
        group_value = df.at[row_idx, group_by_column]
        group_df = df[df[group_by_column] == group_value]
        suggestion = _summarize(group_df[column], is_numeric, min_samples=2)
        if suggestion is not None:
            return suggestion, (0.75 if is_numeric else 0.7)

    # Grup bulunamazsa global istatistiğe düş - ama güven skorunu düşür
    suggestion = _summarize(df[column], is_numeric, min_samples=1)
    if suggestion is None:
        return None, 0.0
    return suggestion, (0.45 if is_numeric else 0.4)


def _summarize(series: pd.Series, is_numeric: bool, min_samples: int) -> Optional[str]:
    """Sayısal kolonda medyan, diğerlerinde en sık görülen değer; yeterli örnek yoksa None."""
    if is_numeric:
        numbers = _to_numeric(series)
        if len(numbers) < min_samples:
            return None
        return _format_number(numbers.median())

    values = _non_empty(series)
    if len(values) < min_samples:
        return None
    mode = values.mode()
    return None if mode.empty else str(mode.iloc[0])
