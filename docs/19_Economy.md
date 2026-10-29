# 19 — Economy

**Document:** Game Design · HADAL  
**Language:** TR (canonical design voice)

---

## Kaynak Ekonomisi

| Kaynak | Rol | Kaynak doc |
|--------|-----|------------|
| **Oxygen** | Hayatta kalma — biterse üs verimini kaybeder (ölmez) | [06_Resources.md](./06_Resources.md), [35_Survival_Loop.md](./35_Survival_Loop.md) |
| **Energy** | Reaktör üretimi; üreticileri besler | [06_Resources.md](./06_Resources.md), [35_Survival_Loop.md](./35_Survival_Loop.md) |
| **Biomass** | Canlı organizma türevi | [06_Resources.md](./06_Resources.md) |
| **Titanium** | İnşaat | [06_Resources.md](./06_Resources.md) |
| **Hadalite** | Premium late-game; en derin bölgeler | [06_Resources.md](./06_Resources.md) |

---

## Hayatta Kalma Döngüsü

Enerji üreticileri besler, mürettebat ve askerler oksijen solur, ara sıra bir sızıntı ya da arıza çıkar. Ceza her zaman toparlanabilir: enerji deposu boşken üreticiler arz/talep oranında (en az %50) çalışır; oksijen 0'da ve düşerken üretim −%20, yeni zamanlayıcılar +%20; hiçbir bina ya da asker kaybolmaz. Ayrıntı ve katalog anahtarları: [35_Survival_Loop.md](./35_Survival_Loop.md).

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
