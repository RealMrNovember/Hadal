# 36 — Telefon Bildirimleri

**Document:** Game Design · HADAL
**Language:** TR (canonical design voice)
**Status:** Aşama 1 uygulandı (2026-10-30): telefonda zamanlanan yerel bildirimler, ayarlar, tercihlerin sunucuda saklanması. Aşama 2 (sunucudan push, Firebase Cloud Messaging) sunucu tarafında hazır, Firebase kimlik bilgileri gelince açılır.

---

## Neden

Last Asylum tarzı oyunlarda oyuncu çoğu zaman oyunu kapatıp zamanlayıcıların bitmesini bekler. "İnşaat bitti", "kalkanın bitiyor", "saldırı geliyor" bildirimleri oyuncuyu doğru anda geri getirir. Oyun içindeki alarmlar ve kısa bildirimler (toast) bu dokümanla değişmez; telefon bildirimi yalnızca oyun **kapalıyken** (arka plandayken) konuşur.

## 1. Kategoriler

| # | Kategori | Kaynak | Ne zaman |
|---|---|---|---|
| 0 | İnşaat ve araştırma | telefon | inşaat/yükseltme bitti, araştırma bitti |
| 1 | Asker ve iyileştirme | telefon | eğitim bitti, iyileştirme bitti |
| 2 | Seferler | telefon | saldırı seferi hedefe vardı, toplama bitti, ekip üsse döndü |
| 3 | Kalkan | telefon | kalkanın bitmesine 30 dakika kala |
| 4 | Ödüller ve VIP | telefon | yeni UTC gününde VIP sandığı ve giriş ödülü hazır (22:00–09:00 arasına düşerse 09:00'da) |
| 5 | Etkinlikler | telefon | bir etkinliğin bitmesine 1 saat kala |
| 6 | Saldırı uyarıları | sunucu (aşama 2) | üsse düşman seferi çıktı; savaş raporu geldi |
| 7 | Posta | sunucu (aşama 2) | yeni posta (herkese giden duyurular hariç) |
| 8 | İttifak (yardım, ralli) | sunucu (aşama 2) | bir ittifak üyesi yardım istedi; ralli başlattı |
| 9 | Sohbet bahsetmeleri | sunucu (aşama 2) | özel mesaj ya da ittifak sohbetinde `@İsim` |

Numara, `NotificationPrefsDto.DisabledMask` içindeki bittir ve asla değişmez (`shared/HADAL.Shared/Simulation/NotificationRules.cs`). Her kategori Android'de kendi kanalıdır (`hadal_build`, `hadal_attack` …); oyuncu telefon ayarlarından da tek tek kapatabilir. Saldırı ve kalkan kanalları yüksek önemdedir (ekranın üstünde açılır).

Hayatta kalma döngüsü (docs/35) için bildirim yok: acil durumun zararının ne zaman başlayacağını sunucu öngörmüyor (yalnızca kendiliğinden ne zaman biteceğini biliyor), bu yüzden atlandı.

## 2. Telefonda zamanlama (aşama 1)

- Paket: Unity **Mobile Notifications** `com.unity.mobile.notifications` **2.4.3** (Unity 6000.0 ile uyumlu; POST_NOTIFICATIONS iznini manifeste kendisi ekler).
- `PhoneNotifications` (Assets/_Hadal/Scripts/UI) sunucunun gönderdiği zamanlayıcıları okur (`NotificationCandidates`): bina `constructionEndsAt`, aktif araştırmalar, `ArmyDto.Training/Healing`, `MarchListDto`, `PvpStatusDto.ShieldUntilMs`, VIP günlük sıfırlama, `EventListDto`. Sunucu zamanları `ClientStateView.ServerNowMs` ile cihaz saatine çevrilir.
- Oyun **arka plana geçince** (`OnApplicationPause(true)`, odak kaybı, çıkış) plan telefona verilir; oyuna **dönünce** zamanlanmış ve gösterilen bildirimler silinir (oyun içi alarmlar devralır). Plan değişmediyse yeniden zamanlanmaz (imza karşılaştırması).
- `LocalNotificationPlanner` (saf C#, EditMode testli) kuralları:
  - yalnızca oyuncunun açık bıraktığı yerel kategoriler; ana anahtar kapalıysa hiçbiri,
  - 1 dakikadan yakın ve 8 günden uzak zamanlayıcılar atlanır,
  - sessiz saatlere düşen bildirim sessiz saatlerin bitişine kayar,
  - aynı kategoride 5 dakika içinde biten zamanlayıcılar **tek** bildirimde birleşir ("3 görev tamamlandı"), sonuncusu bittiğinde gelir; aynı kategoriden iki bildirim arasında en az 5 dakika olur,
  - en fazla 32 bildirim (en yakınları).
- Metinler oyuncunun oyun dilindedir (`Loc`, 18 dil, `notify.msg.*`); Arapça metin şekillendirilmeden verilir (Android kendisi dizer).
- Küçük ikon: HADAL "H" işareti, saydam zemin üstünde beyaz siluet (`art/logo/make_notify_icon.py` → `Assets/_Hadal/Art/Branding/Notify~/drawable-*/hadal_notify.png`; derlemede `HadalNotificationIcons` APK'nın `res/` klasörüne kopyalar). Büyük ikon uygulama ikonudur (`app_icon`).
- Dokunma: bildirim oyunu açar ve ilgili ekrana götürür (araştırma, harita, VIP, etkinlikler; diğerleri üs).

### İzin (Android 13+)

POST_NOTIFICATIONS izni **uygulama açılışında sorulmaz**. Bir kez sorulur:
- oyuncu en az 90 saniyedir oyundayken ve ilk oturum rehberinin ilk 4 adımı bittiyse (rehber bitmiş ya da hiç olmamışsa da), veya
- oyuncu Ayarlar → Bildirimler'de bir anahtarı açtığında (henüz sorulmadıysa).
Reddedilirse bir daha sorulmaz; Bildirimler sayfasında "Telefon ayarlarını aç" düğmesi görünür. Android 12 ve altı izin istemez.

## 3. Ayarlar

Ayarlar (avatar → Ayarlar, docs/26) → **Bildirimler** sayfası:
- **Bildirimler** ana anahtarı,
- İnşaat ve araştırma · Asker ve iyileştirme · Seferler · Kalkan · Ödüller ve VIP · Etkinlikler,
- **Sunucu uyarıları** başlığı altında gri ve "Yakında" yazan: Saldırı uyarıları · Posta · İttifak (yardım, ralli) · Sohbet bahsetmeleri (aşama 2 açılınca etkinleşir),
- **Sessiz saatler** anahtarı (varsayılan 23:00–08:00), başlangıç 21/22/23/00, bitiş 06/07/08/09.

Tercihler cihazda (PlayerPrefs, `NotificationPrefsStore`) **ve** sunucuda tutulur: istemci `SetNotificationPrefs` (CommandType 70) komutunu oyun dili ve UTC farkıyla gönderir; sunucu oyuncu belgesine (`hadal.players.extras` → `Notify`) yazar ve anlık görüntüde (`StateSnapshot.Notifications`, alan 16) ve `base:notify` varlığında geri yollar. Yeni bir telefon (ya da yeniden kurulum) cihazda tercih yoksa sunucudakini benimser; cihazda tercih varsa cihaz kazanır ve sunucuya yazar.

## 4. Sunucu push'u (aşama 2, hazır)

- `PushService` (server/Hadal.Game/PushNotifications): oyun sistemleri `Notify(oyuncu, tür, isim)` der; istekler kuyruğa girer ve arka planda teslim edilir. Gönderim yalnızca şu durumda olur:
  - push yapılandırılmış (varsayılan gönderici `NoopPushSender`: hiçbir şey kuyruğa bile girmez),
  - oyuncu **çevrimdışı** (çevrimiçi oyuncu oyun içi alarmı görür),
  - oyuncunun tercihi kategoriye izin veriyor ve sessiz saatlerinde değil (sunucu tarafında sessiz saat bildirimi **düşürür**; geç gelen saldırı uyarısı işe yaramaz),
  - aynı kategoride son 5 dakikada bu oyuncuya push gitmemiş.
- Kayıtlı telefonlar: `RegisterPushToken` (CommandType 71) — cihaz kimliği + FCM jetonu; oyuncu başına en fazla 5 telefon, aynı telefonun yeni jetonu eskisinin yerine geçer, boş jeton telefonu siler. Firebase'in "UNREGISTERED" dediği jeton bir daha denenmez ve silinir.
- Bağlantı noktaları (küçük kancalar): `WorldService.HandlePvp` (düşman seferi çıktı, yeni ralli → ittifak), `MailService.SendAsync` (posta; savaş raporu postası = saldırı sonucu; 50'den fazla alıcılı duyurular push'lanmaz), `SocialHub` (yardım isteği, özel mesaj ve `@İsim`).
- Metinler sunucuda 18 dilde (`PushTexts`), oyuncunun tercihine kaydettiği oyun dilinde.
- `FcmPushSender`: FCM HTTP v1. Servis hesabıyla JWT (RS256) imzalayıp OAuth erişim jetonu alır (süresinden 5 dk önce yeniler), `projects/<id>/messages:send` adresine telefon başına bir mesaj yollar: bildirim başlık/metin, Android kanalı (`hadal_attack` …), küçük ikon `hadal_notify`, `data.screen` (açılacak ekran).

### Sunucu ortam değişkenleri

| Değişken | Değer |
|---|---|
| `HADAL_FCM_SERVICE_ACCOUNT_JSON_PATH` | CT'de servis hesabı JSON anahtarının yolu, ör. `/opt/hadal/server/secrets/hadal-fcm.json`. Git'e **asla** girmez. Compose bu dosyayı konteynere salt okunur `/run/secrets/hadal-fcm.json` olarak bağlar. |
| `HADAL_FCM_PROJECT_ID` | `hadal-513122` |

İkisi de doluysa ve anahtar okunabiliyorsa push açılır (günlükte "Push: Firebase Cloud Messaging on for project …"); biri boşsa push kapalı kalır. Gateway konteyneri `app` kullanıcısıyla (uid 1654) çalışır; dosya bu kullanıcı tarafından okunabilir olmalı.

## 5. Kullanıcının yapacakları (Firebase kurulumu)

1. **Firebase'i GCP projesinde açın:** https://console.firebase.google.com → "Proje ekle" → mevcut Google Cloud projesi **`hadal-513122`**'yi seçin (yeni proje açmayın). Google Analytics gerekmez.
2. **Android uygulamasını ekleyin:** Proje genel bakış → Android simgesi →
   - Paket adı: `com.cicibyte.hadal`
   - Uygulama takma adı: HADAL
   - SHA-1: `AF:F1:68:52:02:1C:F1:7B:31:A7:F5:D6:5C:1F:D1:D4:22:27:1A:EF`
3. **`google-services.json` dosyasını indirin.** Bu dosya istemci içindir (gizli değildir ama yine de herkese açık bir yere koymayın); aşama 2'de `Assets/` altına girecek.
4. **Cloud Messaging API'nin açık olduğunu doğrulayın:** Proje ayarları → Cloud Messaging → "Firebase Cloud Messaging API (V1)" etkin olmalı (eski API gerekmez).
5. **Servis hesabı oluşturun:** Google Cloud Console → IAM ve Yönetici → Hizmet hesapları → `hadal-513122` projesinde "Hizmet hesabı oluştur" (ör. `hadal-push`) → rol: **Firebase Cloud Messaging API Admin** (`roles/firebasecloudmessaging.admin`) → oluşturun → hesabın Anahtarlar sekmesi → "Anahtar ekle → Yeni anahtar → JSON" ile JSON'u indirin.
6. **JSON'u yalnızca CT'ye koyun** (bilgisayarda, repoda, sohbette bırakmayın):
   ```
   # CT 160 içinde, root olarak
   install -d -m 0750 -o root -g 1654 /opt/hadal/server/secrets
   install -m 0440 -o root -g 1654 hadal-fcm.json /opt/hadal/server/secrets/hadal-fcm.json
   ```
   ve `/opt/hadal/server/.env` içine:
   ```
   HADAL_FCM_SERVICE_ACCOUNT_JSON_PATH=/opt/hadal/server/secrets/hadal-fcm.json
   HADAL_FCM_PROJECT_ID=hadal-513122
   ```
   sonra gateway'i normal dağıtımla yeniden başlatın.

### İstemci tarafında aşama 2 için gerekenler (henüz eklenmedi)

Firebase Unity SDK ağırdır (External Dependency Manager, Firebase App + Messaging AAR'ları, `google-services` işlemesi) ve şu anki derlemeyi riske atmamak için **eklenmedi**. Temiz bir bağlantı noktası bırakıldı:
- `IPushTokenSource` (Assets/_Hadal/Scripts/UI/PhoneNotifications.cs) — jeton kaynağı yokken oyun push'a hiç kaydolmaz.
- `FirebasePushTokenSource.cs` yalnızca `HADAL_FCM` betik tanımıyla derlenir; Firebase Messaging'in `GetTokenAsync`/`TokenReceived` olaylarını `RegisterPushToken` komutuna bağlar.
- Açmak için: Firebase Unity SDK'dan **FirebaseMessaging.unitypackage** (ve bağımlı FirebaseApp) ekleyin, `google-services.json`'u `Assets/` altına koyun, Player Settings → Android → Scripting Define Symbols'a `HADAL_FCM` ekleyin, derleyin. Sunucu tarafı hazır olduğu için başka değişiklik gerekmez; ayarlardaki "Yakında" satırları o sürümde etkinleştirilir.
- Not: FCM'in gösterdiği bildirime dokunmak uygulamayı açar; `data.screen` ile derin bağlantı aşama 2'de Firebase'in `MessageReceived`/başlangıç niyetiyle bağlanacak.

## 6. Testler

- Sunucu (`NotificationTests`): tercihlerin kalıcılığı ve anlık görüntüde dönmesi, telefon/jeton kuralları, tercihlerin sınırlanması, çevrimdışı oyuncuya kendi dilinde push, çevrimiçi oyuncuya asla, kapalı kategori ve ana anahtar, sessiz saatler, 5 dakikalık birleştirme, postada push ve ölü jeton, yapılandırılmamış push'ta hiçbir şey, `@İsim` eşleşmesi, 18 dilde push metni, FCM JWT imzası ve v1 mesaj biçimi.
- Unity EditMode (`NotificationSchedulerTests`): planlayıcı kuralları, durumdan zamanlayıcı okuma, günlük ödülün geceye düşmemesi, 18 dilde metinler, cihazdaki tercihler.
