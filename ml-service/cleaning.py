"""
Onaylanmış düzeltmeleri (resolutions) orijinal veriye uygulayıp temiz bir DataFrame üreten modül.

Prensip: burada hiçbir "tespit" mantığı yok, sadece C# tarafından "kullanıcı bunu onayladı"
diye gelen kararları uyguluyoruz. Tespit (detection) ve uygulama (application) bilinçli olarak
ayrı tutuldu - kullanıcı onaylamadan hiçbir veri değişmiyor.
"""
import pandas as pd
from typing import List, Dict, Any


def apply_cleaning(df: pd.DataFrame, resolutions: List[Dict[str, Any]]) -> pd.DataFrame:
    """
    resolutions: her biri şu alanları içeren bir liste:
        - type: "Duplicate" | "MissingValue" | "FormatError"
        - row_index, related_row_index, column_name, suggested_value

    Mükerrer kayıtlarda: related_row_index'i (ikinci/tekrar eden satırı) siliyoruz.
    Eksik veri / format hatalarında: ilgili hücreyi suggested_value ile değiştiriyoruz.
    """
    cleaned = df.copy()
    rows_to_drop = set()

    for res in resolutions:
        res_type = res.get("type")

        if res_type == "Duplicate":
            related = res.get("related_row_index")
            if related is not None and related in cleaned.index:
                rows_to_drop.add(related)

        elif res_type in ("MissingValue", "FormatError"):
            row_idx = res.get("row_index")
            column = res.get("column_name")
            suggested = res.get("suggested_value")
            if row_idx in cleaned.index and column in cleaned.columns and suggested is not None:
                cleaned.at[row_idx, column] = suggested

    if rows_to_drop:
        cleaned = cleaned.drop(index=list(rows_to_drop))

    return cleaned.reset_index(drop=True)
