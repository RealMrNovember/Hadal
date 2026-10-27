"""Depo içindeki sabit yollar."""
from pathlib import Path

KOK = Path(__file__).resolve().parents[3]
REHBER = KOK / "docs" / "rehber"
VERI = REHBER / "veri"
KATALOG = KOK / "shared" / "catalog" / "catalog.json"
SABLON = KOK / "tools" / "rehber" / "sablon"
