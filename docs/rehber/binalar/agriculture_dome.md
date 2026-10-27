<!-- Bu sayfa tools/rehber/uret.py tarafından üretilir; elle düzenleme. Kaynak: docs/rehber/veri/binalar.json -->
# Tarım Kubbesi

`agriculture_dome` · Kubbe · 🟡

> Baştan sona ekin dolu bir kubbe.

**İşlev.** Bol gıda ve biyokütle; günlük hasat ödülü.

| Özellik | Değer |
| --- | --- |
| Açılış | 1. örnek Reaktör 7 |
| Adet | 1 |
| Halka | 4–12 |
| En yüksek seviye | 5 |
| Üretim (saatlik) | Sv 1: 3.962 GD · 1.486 BK → Sv 5: 162.599 GD · 60.975 BK |
| Tüketim (saatlik) | Sv 1: 264 EN → Sv 5: 10.840 EN |
| Toplam maliyet (1 → en yüksek) | 16.231.540 Titanyum · 6.492.615 Biyokütle · 930 Süperalaşım |
| Toplam ham süre | 50 g 2 sa 4 dk |
| Toplam güç | 54.086 |

## Görünüm evrimi

| Seviye | Katman | Ad | Görünüm |
| --- | --- | --- | --- |
| 1 | Perçin | Perçinli Tarım Kubbesi | Basık perçinli tarım kubbesi; üç teras, yeşil büyüme halkası. |
| 2 | Basınç | Basınçlı Teras Kubbesi | Kalın basınç camı, teraslar dörde çıkar; yanlarda besin küreleri. |
| 3 | Alaşım | Alaşım Hasat Kubbesi | Süperalaşım kaburgalar, ortada hasat kulesi, eşanjörlü sulama. Yeşil-mor. |
| 4 | Biyomekanik | Kaynaşık Bahçe Kubbesi | Teraslar kitin bir kök ağıyla örülür; kubbenin içi canlı bir bahçe. Turkuaz damarlar. |
| 5 | Rezonans | Işıyan Bahçe | Bahçe kendi ışığını üretir; her nabızda teraslardan yukarı bir ışık dalgası yükselir. Reaktör 50'de Çekirdek'e uyar. |

## Seviye tablosu

Maliyet ve süre o seviyeye **yükseltmenin** bedelidir. Süre hamdır: hız bonusları, ittifak yardımı ve hızlandırma uygulanmadan önceki değer (bkz. [02 · Tempo ve Denge](../02_tempo_ve_denge.md)). Güç, o seviyenin eklediği güçtür.

| Sv | Reaktör | Titanyum | Biyokütle | Süperalaşım | Süre | Güç | Üretim/sa | Kümülatif süre |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | 7 | 1.540 | 615 | — | 4 dk | 314 | 3.962 GD · 1.486 BK | 4 dk |
| 2 | 20 | 130.000 | 52.000 | — | 14 sa | 3.000 | 25.200 GD · 9.450 BK | 14 sa 4 dk |
| 3 | 30 | 1.100.000 | 440.000 | — | 4 g 12 sa | 8.400 | 57.600 GD · 21.600 BK | 5 g 2 sa 4 dk |
| 4 | 40 | 5.000.000 | 2.000.000 | 330 | 17 g | 18.000 | 122.400 GD · 45.900 BK | 22 g 2 sa 4 dk |
| 5 | 45 | 10.000.000 | 4.000.000 | 600 | 28 g | 24.372 | 162.599 GD · 60.975 BK | 50 g 2 sa 4 dk |

## Bugünkü oyunla karşılaştırma (catalog v8)

Kodda bugün: en fazla 1 adet, Reaktör 7 ile açılır, 5 seviye; 1 → 5 toplam ham süre 25 dk 25 sn. Hedef tasarımda aynı yol 50 g 2 sa 4 dk sürer.

| Sv | v8 Titanyum | v8 Biyokütle | v8 süre | Hedef Titanyum | Hedef süre |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 700 | 250 | 4 dk | 1.540 | 4 dk |
| 5 | 988 | 353 | 6 dk 18 sn | 10.000.000 | 28 g |


← [03 · Binalar](../03_binalar.md)
