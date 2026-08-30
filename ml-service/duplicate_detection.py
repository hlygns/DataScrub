"""
Mükerrer kayıt tespiti modülü.

Yaklaşım: Her satır çiftini karşılaştırmak yerine (O(n^2), büyük dosyalarda çok yavaş),
önce "blocking" yapıyoruz - örn. aynı ilk harfle başlayan isimleri gruplayıp sadece
o gruplar içinde karşılaştırma yapıyoruz. Bu, gerçek dünya veri mühendisliğinde
kullanılan klasik bir performans tekniği (Record Linkage / Entity Resolution).
"""
import pandas as pd
from rapidfuzz import fuzz
from typing import List, Dict, Any


def normalize_text(value: Any) -> str:
    """Türkçe karakter ve boşluk farklılıklarını normalize et."""
    if pd.isna(value):
        return ""
    text = str(value).strip().lower()
    tr_map = str.maketrans("çğıöşü", "cgiosu")
    return text.translate(tr_map)


def detect_duplicates(
    df: pd.DataFrame,
    match_columns: List[str],
    threshold: float = 85.0,
) -> List[Dict[str, Any]]:
    """
    match_columns: karşılaştırılacak kolonlar, örn. ["ad_soyad", "email", "telefon"]
    threshold: 0-100 arası benzerlik skoru eşiği (RapidFuzz skalası)

    Dönüş: her mükerrer çift için bir issue kaydı, confidence_score 0.0-1.0 arası normalize edilmiş.
    """
    issues: List[Dict[str, Any]] = []
    n = len(df)

    # Blocking: ilk kolonun ilk karakterine göre grupla, sadece aynı grup içinde karşılaştır
    blocking_key = match_columns[0]
    df["_block"] = df[blocking_key].apply(lambda v: normalize_text(v)[:1] if pd.notna(v) else "")

    for block_value, group in df.groupby("_block"):
        indices = group.index.tolist()
        for i in range(len(indices)):
            for j in range(i + 1, len(indices)):
                idx_a, idx_b = indices[i], indices[j]
                scores = []
                for col in match_columns:
                    val_a = normalize_text(df.at[idx_a, col])
                    val_b = normalize_text(df.at[idx_b, col])
                    if val_a and val_b:
                        scores.append(fuzz.token_sort_ratio(val_a, val_b))

                if not scores:
                    continue

                avg_score = sum(scores) / len(scores)
                if avg_score >= threshold:
                    issues.append({
                        "row_index": int(idx_a),
                        "related_row_index": int(idx_b),
                        "column_name": ", ".join(match_columns),
                        "original_value": str(df.loc[idx_a, match_columns].to_dict()),
                        "suggested_value": f"Satır {idx_b} ile mükerrer olabilir",
                        "confidence_score": round(avg_score / 100.0, 3),
                    })

    df.drop(columns=["_block"], inplace=True)
    return issues
