# 04 — Gameplay Overview

**Document:** Game Design · HADAL  
**Language:** TR (canonical design voice)

---

## Ana Oyun Döngüsü

HADAL, **survival-strategy MMO** döngüsü üzerine kuruludur:

| Aşama | Oyuncu eylemi | Sistem |
|-------|---------------|--------|
| 1 | Üs kurar | [05_Base_Building.md](./05_Base_Building.md) |
| 2 | Kaynak üretir | [06_Resources.md](./06_Resources.md) |
| 3 | Denizaltı inşa eder | Base · Tech · [15_Monetization.md](./15_Monetization.md) (Sonar Ping) |
| 4 | Derin keşif | [08_Expeditions.md](./08_Expeditions.md) |
| 5 | Teknoloji bulur | Expeditions · Gacha · Research |
| 6 | Hero kazanır | [10_Heroes.md](./10_Heroes.md) · Cryo-Pod Salvage |
| 7 | Kubbe genişletir | Base Building · Mega Domes |
| 8 | Canavarlarla savaşır | [09_Enemies.md](./09_Enemies.md) |
| 9 | PvP | [12_PvP_Alliance.md](./12_PvP_Alliance.md) |
| 10 | Alliance | Underwater City · Kuşatmalar |
| 11 | The Core | [20_End_Game.md](./20_End_Game.md) |

---

## Baskı ve Derinlik

Tüm keşif ve savaş döngüsü **[07_Pressure_System.md](./07_Pressure_System.md)** ile sınırlanır — derinlik arttıkça risk, ödül ve drama artar.

---

## Teknik Oynanış Kuralı (v3)

Tüm gameplay eylemleri **Command → Server validate → StateDelta → Client view** akışındadır.

Detay: [17_Technical_Architecture.md](./17_Technical_Architecture.md)
