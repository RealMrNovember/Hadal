# 26 — Ana Ekran (HUD)

**Document:** UI · HADAL
**Language:** TR (canonical design voice)
**Status:** İlk iskelet uygulandı (2026-10-26, istemci 1.5.0). Referans: Last Asylum: Plague dikey ekranı; araştırma özeti `25_Heroes_Alliance_Chat.md` sonrası tür incelemesi.

---

## Yerleşim (dikey telefon, güvenli alan içinde)

```
+--------------------------------------------------+
|[Avatar] [Titanyum][Biyokütle][Gıda]   [Mağaza 💎]|  başlık 1. satır
|[ Sv.7 ] [Oksijen ][Enerji   ][Güç  ]             |  başlık 2. satır
|▼ Kuyruk 1/2                              [Kapsül]|  sağ: etkinlik sütunu
| [🔨 Reaktör → Sv.8   12:05]              [Etkin.]|
| [⚗ Depo Müh.         03:10]              [Kuşat.]|
|                                                  |
|[Genişlet]          ÜS GÖRÜNÜMÜ          [İttifak]|  sol: kısayollar
|[Araştırma]                              [Posta  ]|  sağ alt: ittifak/posta/çanta
|[Yardım  ]                               [Çanta  ]|
|[Ara     ]                                        |
|[📜 Yükselt: Oksijen Tesisi → Sv. 5]              |  önerilen görev
+--------------------------------------------------+
|[KAHRAMAN]|  son 3 sohbet satırı          |[BÖLGE]|  alt çubuk
+--------------------------------------------------+
```

- **Avatar** ayarları ve hesabı açar (köşedeki dişli kalktı). Altındaki rozet Çekirdek Reaktör seviyesidir.
- **Kaynak çipleri:** dokununca miktar / sınır ve dakikalık net üretim. Sıfırda kırmızı, sınırda turuncu.
- **Güç:** bina seviyeleri × 10 + kahraman güçleri (sunucuyla aynı formül).
- **Mağaza:** Hadalit bakiyesi; mağaza gelene kadar Kriyo-Kapsül'ü açar.
- **Etkinlik sütunu:** Kriyo-Kapsül (ücretsiz çekim sayacı, hazırken kırmızı nokta), Etkinlik ve Kuşatma (yakında).
- **Kuyruk** (Last Asylum'daki "Takım" paneli): süren inşaat, araştırma ve kurtarma görevleri, kalan süreyle; ücretsiz bitirme süresine girince camgöbeği. Dokununca ilgili bina açılır. Başlığa dokunmak paneli açar/kapatır.
- **Sol kısayollar:** Genişlet (halka açma), Araştırma (Araştırma Merkezi), Yardım (ittifak yardım sekmesi, bekleyen istek varsa kırmızı nokta), Ara (yakında).
- **Sağ alt:** İttifak, Posta ve Çanta (son ikisi yakında).
- **Önerilen görev:** reaktörün gerisinde kalan en düşük seviyeli bina, yoksa reaktör. Dokununca o bina açılır. Gerçek görev sistemi (bölüm görevleri, günlük görevler) gelince bunun yerini alır.
- **Alt çubuk:** solda büyük Kahraman düğmesi (ücretsiz çekim ya da yıldız yükseltme hazırsa kırmızı nokta), ortada son üç sohbet satırı (dokununca sohbet, okunmamış varsa nokta), sağda Bölge (dünya haritası, Phase 11).
- Bir panel açıkken başlık görünür kalır; başlığın altındaki her şey gizlenir.

## Teknik Not

- `Assets/_Hadal/Scripts/UI/Hud/`: `MainHudView` (iskelet), `SafeArea` (çentik ve hareket çubuğu), `HudIcons` (Resources/Icons'tan sprite).
- Simgeler kodla üretilir: `art/blender/icons.py` (Blender 4.2, Cycles, şeffaf 256 px, Freestyle dış çizgi). Değiştirmek için betiği düzenleyip yeniden çalıştır; `HadalIconImporter` içe aktarma ayarlarını verir.
- HUD, `BaseManagementView` tarafından kurulur ve `SocialView` sayfalarını açar. Eski kaynak listesi (`ResourceHudView`), dişli ve "Üssü Genişlet" düğmesi gizlenir.
- İncelenen hazır parçalar: EmpireWars (alt menü + bottom sheet, güvenli alan, kamera, altıgen dünya haritası — Phase 11'de kullanılacak), Mudit (ekran yığını, servisler — para kazanma fazında).
