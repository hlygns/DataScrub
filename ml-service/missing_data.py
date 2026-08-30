"""
Eksik veri tespiti modülü.

Yaklaşım: Sadece "boş" demekle kalmıyoruz - kategorik kolonlarda en sık görülen değeri,
sayısal kolonlarda medyanı öneriyoruz. Eğer satırda "grup belirleyici" bir kolon varsa
(örn. şehir), o gruptaki değerlerden öneri üretmek daha isabetli (global ortalama yerine).
"""
import pandas as pd
from typing import List, Dict, Any, Optional


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

        is_numeric = pd.api.types.is_numeric_dtype(df[column])

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
    if group_by_column and group_by_column in df.columns and pd.notna(df.at[row_idx, group_by_column]):
        group_value = df.at[row_idx, group_by_column]
        group_df = df[df[group_by_column] == group_value]
        non_null = group_df[column].dropna()
        if len(non_null) >= 2:  # anlamlı bir örneklem olsun
            if is_numeric:
                return round(non_null.median(), 2), 0.75
            else:
                mode = non_null.mode()
                if not mode.empty:
                    return mode.iloc[0], 0.7

    # Grup bulunamazsa global istatistiğe düş - ama güven skorunu düşür
    non_null = df[column].dropna()
    if non_null.empty:
        return None, 0.0

    if is_numeric:
        return round(non_null.median(), 2), 0.45
    else:
        mode = non_null.mode()
        if mode.empty:
            return None, 0.0
        return mode.iloc[0], 0.4
