"""
Format tutarsızlığı tespiti modülü.

Kolon adına bakarak hangi kuralın uygulanacağına karar veriyoruz (tarih, telefon, isim vb.)
Basit ama genişletilebilir bir "kural motoru" yaklaşımı - yeni format tipi eklemek
sadece yeni bir fonksiyon yazıp DETECTORS sözlüğüne eklemek kadar kolay (Open/Closed prensibi).
"""
import re
import pandas as pd
from datetime import datetime
from typing import List, Dict, Any, Optional, Tuple

DATE_FORMATS = ["%d/%m/%Y", "%d-%m-%Y", "%Y-%m-%d", "%m/%d/%Y", "%d.%m.%Y"]


def _try_parse_date(value: str) -> Optional[datetime]:
    for fmt in DATE_FORMATS:
        try:
            return datetime.strptime(value.strip(), fmt)
        except ValueError:
            continue
    return None


def _check_date_column(value: Any) -> Optional[Tuple[str, float]]:
    if pd.isna(value):
        return None
    text = str(value).strip()
    parsed = _try_parse_date(text)
    if parsed is None:
        return None
    standardized = parsed.strftime("%Y-%m-%d")  # ISO 8601 standardına çeviriyoruz
    if standardized != text:
        return standardized, 0.9
    return None


def _check_phone_column(value: Any) -> Optional[Tuple[str, float]]:
    if pd.isna(value):
        return None
    text = str(value).strip()
    digits = re.sub(r"\D", "", text)

    # Türkiye telefon numarası normalizasyonu: 05XX XXX XX XX -> +90 5XX XXX XX XX
    if digits.startswith("0") and len(digits) == 11:
        digits = "90" + digits[1:]
    elif len(digits) == 10 and digits.startswith("5"):
        digits = "90" + digits

    if len(digits) != 12 or not digits.startswith("90"):
        return None  # tanınmayan format, dokunma

    standardized = f"+{digits[:2]} {digits[2:5]} {digits[5:8]} {digits[8:10]} {digits[10:12]}"
    if standardized != text:
        return standardized, 0.85
    return None


def _check_name_column(value: Any) -> Optional[Tuple[str, float]]:
    if pd.isna(value):
        return None
    text = str(value).strip()
    # Tamamı büyük harf ya da tamamı küçük harfse Title Case'e çevir
    if text.isupper() or text.islower():
        standardized = text.title()
        if standardized != text:
            return standardized, 0.6
    return None


DETECTORS = {
    "date": _check_date_column,
    "phone": _check_phone_column,
    "name": _check_name_column,
}


def _guess_column_type(column_name: str) -> Optional[str]:
    name = column_name.lower()
    if any(k in name for k in ["tarih", "date", "dogum"]):
        return "date"
    if any(k in name for k in ["telefon", "phone", "gsm", "tel"]):
        return "phone"
    if any(k in name for k in ["ad", "isim", "name", "soyad"]):
        return "name"
    return None


def detect_format_errors(df: pd.DataFrame) -> List[Dict[str, Any]]:
    issues: List[Dict[str, Any]] = []

    for column in df.columns:
        column_type = _guess_column_type(column)
        if column_type is None:
            continue

        detector = DETECTORS[column_type]
        for idx, value in df[column].items():
            result = detector(value)
            if result is None:
                continue
            suggested_value, confidence = result
            issues.append({
                "row_index": int(idx),
                "related_row_index": None,
                "column_name": column,
                "original_value": str(value),
                "suggested_value": suggested_value,
                "confidence_score": confidence,
            })

    return issues
