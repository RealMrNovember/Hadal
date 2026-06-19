# 19 — Economy

**Document:** Game Design · HADAL  
**Language:** TR (canonical design voice)

---

## Kaynak Ekonomisi

| Kaynak | Rol | Kaynak doc |
|--------|-----|------------|
| **Oxygen** | Hayatta kalma — biterse üs ölür | [06_Resources.md](./06_Resources.md) |
| **Energy** | Reaktör üretimi | [06_Resources.md](./06_Resources.md) |
| **Biomass** | Canlı organizma türevi | [06_Resources.md](./06_Resources.md) |
| **Titanium** | İnşaat | [06_Resources.md](./06_Resources.md) |
| **Hadalite** | Premium late-game; en derin bölgeler | [06_Resources.md](./06_Resources.md) |

---

## Hadalite — Premium Döngü

**Hadalite** sadece en derin bölgelerde bulunur ve oyunun **premium late-game elementi**dir.

Kullanım alanları:

- **Cryo-Pod Salvage** (kahraman gacha)
- **Deep Sea Sonar Ping** (ekipman / blueprint gacha)
- İleri basınç ve end-game teknolojileri

Tüm harcama ve drop sonuçları **sunucu otoritelidir**.

Detay: [15_Monetization.md](./15_Monetization.md)

---

## Alliance Ekonomisi

Alliance **Trade Station** ile kaynak ve blueprint takası — kuşatma meta ekonomisi.

Detay: [12_PvP_Alliance.md](./12_PvP_Alliance.md)

---

## Teknik Kural (v3)

Ekonomi state'i PostgreSQL'de tutulur; client yalnızca `VisualStateCache` görüntüler.

Detay: [17_Technical_Architecture.md](./17_Technical_Architecture.md)
