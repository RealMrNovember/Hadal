"""catalog v8 formülleri; shared/HADAL.Shared/Simulation/*.cs ile birebir aynı.

Python'un round() fonksiyonu, .NET Math.Round'un varsayılanı gibi yarımı çifte yuvarlar.
"""


def bina(katalog, bina_id):
    for b in katalog["buildings"]:
        if b["id"] == bina_id:
            return b
    return None


def maks_seviye(katalog, b):
    return b.get("maxLevel") or katalog["progression"]["maxLevel"]


def bina_bedel(katalog, b, seviye):
    """BuildingProgression.Cost: taban × costGrowth^(L−1)."""
    carpan = katalog["progression"]["costGrowth"] ** max(0, seviye - 1)
    return {c["resource"]: int(round(c["amount"] * carpan)) for c in b["buildCost"]}


def bina_sure(katalog, b, seviye):
    """BuildingProgression.Seconds: taban × timeGrowth^(L−1)."""
    return int(round(b["buildSeconds"] * katalog["progression"]["timeGrowth"] ** max(0, seviye - 1)))


def bina_uretim_saat(b, seviye, alan="production"):
    """ProductionFormulas: tick başına taban × (100 + 25·(L−1)) / 100; tick = 1 sn."""
    yuzde = 100 + 25 * (seviye - 1)
    return {c["resource"]: c["amount"] * 3600 * yuzde // 100 for c in b.get(alan, [])}


def arastirma_dugumu(katalog, dugum_id):
    for n in katalog["research"]["nodes"]:
        if n["id"] == dugum_id:
            return n
    return None


def arastirma_seviye(katalog, n, seviye):
    """ResearchRules.Cost / BaseSeconds ve merkez seviyesi: gerekli + adım·(L−1)."""
    r = katalog["research"]
    bedel = {c["resource"]: int(round(c["amount"] * r["costGrowth"] ** max(0, seviye - 1))) for c in n["baseCost"]}
    sure = int(round(n["baseSeconds"] * r["timeGrowth"] ** max(0, seviye - 1)))
    merkez = n["requiredCenterLevel"] + r["centerStep"] * (seviye - 1)
    return {"bedel": bedel, "sure": sure, "merkez": merkez}


def kahraman(katalog, kahraman_id):
    for h in katalog["heroes"]["list"]:
        if h["id"] == kahraman_id:
            return h
    return None


def _bol_sifira(a, b):
    """C# tamsayı bölmesi (sıfıra doğru keser)."""
    q = abs(a) // abs(b)
    return q if (a >= 0) == (b > 0) else -q


def kahraman_etki(h, seviye, yildiz):
    """HeroRules.EffectValue."""
    return (h["effectBase"] + _bol_sifira(max(0, seviye - 1) * h["effectPerLevelTenths"], 10)
            + max(0, yildiz - 1) * h["effectPerStar"])


def kahraman_guc(katalog, h, seviye, yildiz):
    """HeroRules.Power."""
    nad = {r["id"]: r for r in katalog["heroes"]["rarities"]}[h["rarity"]]
    return int(round(nad["basePower"] * (1 + max(0, seviye - 1) * 0.1) * (1 + max(0, yildiz - 1) * 0.2)))


def kahraman_egitim(katalog, h, seviye):
    """HeroRules.LevelCost: seviye → seviye+1."""
    hr = katalog["heroes"]
    nad = {r["id"]: r for r in hr["rarities"]}[h["rarity"]]
    carpan = hr["levelCostGrowth"] ** max(0, seviye - 1) * max(0.1, nad["costMultiplier"])
    return {c["resource"]: int(round(c["amount"] * carpan)) for c in hr["levelCost"]}
