# HADAL Oyun Kılavuzu

*Beneath The Abyss. Humanity Begins Again.*

Hadal'ın tam tasarım kılavuzu: her binanın seviye seviye maliyeti, süresi ve gücü; her kahramanın
yetenekleri ve yıldız eşikleri; her araştırma dalı; asker, denizaltı, harita, ittifak, VIP, The
Syndicate, etkinlikler, sezonlar ve son oyun. Last Asylum ve Rise of Kingdoms'un sistemlerinden
yapı ve mekanik alır, gerçek derin deniz bilimine dayanır ve Hadal'ın kendi evrenini kurar.

- **Bina:** <!--v:hesap.sayilar.bina_turu-->34<!--/v--> tür, <!--v:hesap.sayilar.bina_ornegi-->67<!--/v--> örnek, 50 seviye
- **Araştırma:** <!--v:hesap.sayilar.arastirma_dali-->13<!--/v--> dal, <!--v:hesap.sayilar.teknoloji-->154<!--/v--> teknoloji, <!--v:hesap.sayilar.arastirma_seviyesi-->1.138<!--/v--> seviye
- **Kahraman:** <!--v:hesap.sayilar.kahraman-->30<!--/v-->
- **Tempo:** ücretsiz oyuncu Reaktör 50'ye <!--v:hesap.sim.f2p.gun_50|gun-->368,1. gün<!--/v--> ulaşır

## Bölümler

| # | Bölüm | İçerik |
|---|---|---|
| 00 | [Kanon](00_kanon.md) | Bağlayıcı kararlar; eski dökümanlarla çelişkilerin çözümü |
| 01 | [Temel Kavramlar](01_temel_kavramlar.md) | Dünya, çekirdek döngü, kaynaklar, tasarım ilkeleri, nasıl okunur |
| 02 | [Tempo ve Denge](02_tempo_ve_denge.md) | Bir yıllık tempo, eğriler, formüller, tavanlar, güç, simülasyon |
| 03 | [Binalar](03_binalar.md) | Halkalar, kurallar, tüm binalar, rol eşleşmesi |
| 04 | [Bina Evrimi](04_bina_evrimi.md) | Her 10 seviyede değişen görünüm, ek modüller, sanat bütçesi |
| 05 | [Kahramanlar](05_kahramanlar.md) | Kadro, istatistik, yetenek, yıldız, yetenek ağacı, Kriyo-Kapsül |
| 06 | [Araştırma](06_arastirma.md) | İki laboratuvar, 13 dal, bütçe |
| 07 | [Birlikler ve Savaş](07_birlikler_ve_savas.md) | Asker türleri ve kademeleri, savaş çözümü, kayıplar |
| 08 | [Denizaltılar, Basınç, Seferler](08_denizaltilar_basinc_seferler.md) | Hangarlar, basınç sınıfları, Derinlik Haritası, sis |
| 09 | [Dünya Haritası ve PvP](09_dunya_haritasi_ve_pvp.md) | Kuşaklar, yataklar, PvE, yürüyüş, ralli, kalkan, Egemen |
| 10 | [İttifak](10_ittifak.md) | Rütbeler, teknoloji, kredi, bölge, yardım, hediye |
| 11 | [VIP](11_vip.md) | Kalıcı VIP seviyeleri ve ayrıcalıkları |
| 12 | [The Syndicate](12_syndicate.md) | Paketler, aylık kart, sezon bileti, büyüme fonu, hızlandırma |
| 13 | [Etkinlikler ve Sezonlar](13_etkinlikler_ve_sezonlar.md) | Takvim, ittifak savaşları, Akıntı Savaşı, doğa olayları |
| 14 | [Son Oyun](14_son_oyun.md) | Çekirdek, Hadal Titanı, Reaktör 50 sonrası |
| 15 | [Ekonomi Akışı](15_ekonomi_akisi.md) | Kaynak kaynakları ve harcamaları, F2P gelir dökümü |
| 16 | [Kronik, Köken, Profil](16_kronik_kokenler_profil.md) | Derinlik Kroniği, 5 köken, kaptan profili, göç |
| 17 | [Teçhizat](17_techizat.md) | Malzemeler, nadirlikler, setler |
| 18 | [Bilim Temeli](18_bilim_temeli.md) | Hadal'ın dayandığı gerçek okyanus verisi ve kaynakça |
| 19 | [Ordu, Sefer ve Geçişler: Uygulama](19_ordu_sefer_ve_gecisler_uygulama.md) | 07–09'un oyunda nasıl görüneceği: eğitim, Revir, takımlar, sefer, hızlandırma, açılış ve geçişler; paket sırası |
| 90 | [Durum ve Yol Haritası](90_durum_ve_yol_haritasi.md) | Kodda ne var, ne sırayla yapılır |
| — | [Sözlük](sozluk.md) | Terimler ve kimlikler |

## Veritabanı sayfaları (üretilir)

- **Binalar:** [binalar/](binalar/) — her bina için 50 satırlık seviye tablosu. Başlangıç: [Çekirdek Reaktör](binalar/core_reactor.md).
- **Kahramanlar:** [kahramanlar/](kahramanlar/) — kadro için bkz. [05](05_kahramanlar.md).
- **Araştırma dalları:** [arastirma/](arastirma/) — dallar için bkz. [06](06_arastirma.md).
- **Tempo simülasyonu:** [uretilen/simulasyon.md](uretilen/simulasyon.md).
- **Etkileşimli veritabanı:** `web/hadal-veritabani.html` — bina, kahraman ve araştırma tarayıcısı ile
  yükseltme hesaplayıcısı (tek dosya; tarayıcıda açılır).

## Durum etiketleri

✅ kodda ve aynı · 🟡 kodda ama farklı ya da işlevsiz · ⬜ tasarım

## Kılavuzu güncellemek

Kılavuzun tek veri kaynağı [`veri/`](veri/) klasörüdür; şemalar [`veri/SEMA.md`](veri/SEMA.md)'de.

```bash
python tools/rehber/uret.py
```

```bash
python tools/rehber/uret.py --html
```

```bash
python -m unittest tools/rehber/test_rehber.py
```

İlki tüm sayfaları ve tabloları yeniden üretip kontrolleri çalıştırır (ikinci çalıştırma hiçbir
dosyayı değiştirmez); ikincisi web veritabanını da üretir; üçüncüsü formül testlerini çalıştırır.
