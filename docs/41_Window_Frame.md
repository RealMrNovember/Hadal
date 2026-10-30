# 41 — Pencere Çerçevesi ve Arayüz Yenilemesi

**Document:** UI · HADAL
**Language:** TR (canonical design voice)
**Status:** Çerçeve yazıldı (`UiWindow`), ilk taşınan pencere bina yükseltme (2026-10-30). Referans: Last Asylum yükseltme, posta, profil, ayarlar ve sıralama pencereleri (`docs/37`, `docs/38`, `docs/40`).

---

## 1. Sorun

Kullanıcının tespiti: oyunun dizilişi ve pencere yerleşimleri "10 yıl önceki teknoloji" gibi duruyor. Denetim (`docs/40`) ile aynı sonuç:

- Her pencere ekran yüzdeleriyle elle yerleştirilmiş (`Place(0.04, 0.17, 0.96, 0.83)`). Kutular ekranla esniyor, yazı sabit kalıyor; içerik üst çeyrekte, altı boş.
- Kalın, dış çizgili eski `Text` her yerde; 25 farklı yazı boyu.
- Pencere arkası düz karartma; açılış/kapanış geçişi yok ya da kaba.
- Bölüm başlıkları koyu hap, şart satırları siyah kutu, kapat düğmesi kırmızı daire: her pencere farklı bir dilden.

## 2. Last Asylum'un pencere dili

| Öğe | LA | HADAL (yeni) |
|---|---|---|
| Arka plan | Oyun bulanık ve koyu | Ekran görüntüsü 1/16'ya küçültülüp bulanık, koyu ton (`UiWindow.Backdrop`) |
| Çerçeve | Lacivert, ince kenar | `header` kaplaması tüm kartı sarar |
| Başlık | Ortada büyük beyaz, altında "Sv.24" | `TypeScale.Display` + `Subtitle` |
| Kapat | Sade beyaz çarpı | Çizilmiş beyaz çarpı, 120 px dokunma alanı |
| Gövde | Açık gri-mavi, satırlar zebra | `panel` + `ZebraA/ZebraB` |
| Bölüm | İnce açık mavi bant, ortalı normal yazı | `Section()` |
| Eksik şart | Kırmızı yazı + yeşil "Git" | `Need(icon, text, met, "Git", action)` |
| Boyut | İçerik kadar | `Fit()`: gövde içerik kadar, ekranın %60'ını aşınca kayar |
| Alt | Not (orijinal süre) + 1–2 büyük düğme | `Note()` + `AddButton(label, detail, …)` |
| Hareket | Kısa büyüme + solma | 0,22 sn ease-out-back açılış, 0,14 sn kapanış |

## 3. Kurallar (yeni ve taşınan her pencere)

1. Pencere `UiWindow.Open(...)` ile açılır; konum ve boyut yazılmaz, satırlar `Body`'ye eklenir, sonunda `Fit()`.
2. Saniyede bir yenilenen pencere yeniden açılmaz: `Clear()` + doldur + `Fit()`.
3. Açık zeminde koyu mürekkep (`UiWindow.Ink`, `InkGood`, `InkBad`); dış çizgi yalnızca lacivert başlıkta.
4. Yazı boyları yalnızca `UiKit.TypeScale`'den.
5. Bir eksik varsa yanında onu çözen düğme olur ("Git"); oyuncu pencereyi kapatıp aramaz.
6. Tam ekran sayfalar (posta, ittifak, kahramanlar) için aynı dilde ikinci bir kalıp gelecek: üstte geri oklu başlık çubuğu, sekmeler, kart listesi, altta sabit eylem çubuğu.

## 4. Sıra

1. Altyapı: TextMeshPro geçişi (ayrı dalda, tüm arayüz), `UiWindow` (bu belge).
2. Bina yükseltme penceresi ✅ (ilk örnek).
3. Eğitim, hızlandırma, bina ayrıntıları.
4. Tam ekran sayfa kalıbı → çanta, kahramanlar, ittifak, posta, ayarlar, profil.
5. Dünya haritası pencereleri (hedef menüsü, sefer gönderme).
