using System.Collections.Generic;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Minimal UI strings in Turkish and English, picked from the device language. A placeholder
    /// until the localization pass (18 languages, Phase 17); keys stay stable for that migration.
    /// </summary>
    public static class Loc
    {
        private static readonly Dictionary<string, (string Tr, string En)> Strings = new()
        {
            ["title.tagline"] = ("İnsanlığın son kubbesi, okyanusun dibinde.", "Humanity's last dome, at the bottom of the ocean."),
            ["title.connecting"] = ("Sunucuya bağlanılıyor…", "Connecting to the server…"),
            ["title.reconnecting"] = ("Bağlantı koptu, yeniden deneniyor…", "Connection lost, retrying…"),
            ["title.maintenance"] = ("Sunucu bakımda. Birazdan tekrar denenecek.", "The server is under maintenance. Retrying shortly."),
            ["title.update"] = ("Yeni bir sürüm gerekli. Lütfen güncelleyin.", "A newer version is required. Please update."),
            ["title.tap"] = ("Başlamak için dokun", "Tap to start"),

            ["account.title"] = ("Profil ve Hesap", "Profile & Account"),
            ["account.name"] = ("Oyuncu adı", "Player name"),
            ["account.save"] = ("Kaydet", "Save"),
            ["account.saved"] = ("Kaydedildi.", "Saved."),
            ["account.id"] = ("Hesap No", "Account ID"),
            ["account.protected"] = ("Hesabın korunuyor.", "Your account is protected."),
            ["account.unprotected"] = ("Hesabın bu cihaza bağlı. Telefonu değiştirirsen ilerlemen kaybolabilir.", "Your account lives on this device only. Changing phones could lose your progress."),
            ["account.protect"] = ("Hesabı Koru", "Protect Account"),
            ["account.protect.hint"] = ("Bir şifre belirle; sana bir aktarım kodu verelim. Yeni cihazda kod + şifre ile girebilirsin.", "Choose a password and get a transfer code. Use code + password to sign in on a new device."),
            ["account.password"] = ("Şifre (en az 8 karakter)", "Password (8+ characters)"),
            ["account.password2"] = ("Şifre tekrar", "Repeat password"),
            ["account.create"] = ("Kod Oluştur", "Create Code"),
            ["account.code"] = ("Aktarım kodun", "Your transfer code"),
            ["account.code.note"] = ("Bu kodu ve şifreni sakla. Yeni bir kod oluşturursan eskisi geçersiz olur.", "Keep this code and your password. Creating a new code retires the old one."),
            ["account.copy"] = ("Kopyala", "Copy"),
            ["account.copied"] = ("Kopyalandı", "Copied"),
            ["account.switch"] = ("Başka Hesapla Giriş", "Sign In to Another Account"),
            ["account.switch.hint"] = ("Aktarım kodu ve şifreyle giriş yap. Bu cihazdaki hesap korunmuyorsa erişim kaybolur.", "Sign in with a transfer code and password. An unprotected account on this device will be lost."),
            ["account.transfercode"] = ("Aktarım kodu (XXXX-XXXX-XXXX)", "Transfer code (XXXX-XXXX-XXXX)"),
            ["account.signin"] = ("Giriş Yap", "Sign In"),
            ["account.google"] = ("Google ile bağla (yakında)", "Link Google (soon)"),
            ["account.apple"] = ("Apple ile bağla (yakında)", "Link Apple (soon)"),
            ["account.close"] = ("Kapat", "Close"),
            ["account.back"] = ("Geri", "Back"),

            ["base.build.title"] = ("İnşa Et · Halka {0}", "Build · Ring {0}"),
            ["base.build"] = ("İnşa Et", "Build"),
            ["base.upgrade"] = ("Yükselt", "Upgrade"),
            ["base.finish"] = ("Ücretsiz Bitir", "Finish Free"),
            ["base.close"] = ("Kapat", "Close"),
            ["base.level"] = ("Seviye {0}", "Level {0}"),
            ["base.level.max"] = ("En yüksek seviye", "Max level"),
            ["base.constructing"] = ("İnşa ediliyor · {0}", "Under construction · {0}"),
            ["base.upgrading"] = ("Seviye {0}'e yükseltiliyor · {1}", "Upgrading to level {0} · {1}"),
            ["base.produces"] = ("Üretim", "Produces"),
            ["base.consumes"] = ("Tüketim", "Consumes"),
            ["base.next"] = ("Sonraki seviye", "Next level"),
            ["base.cost"] = ("Maliyet", "Cost"),
            ["base.time"] = ("Süre", "Time"),
            ["base.permin"] = ("/dk", "/min"),
            ["base.nothing"] = ("Bu yuvaya şu an inşa edilebilecek bina yok.", "Nothing can be built on this slot yet."),
            ["base.started"] = ("İnşaat başladı.", "Construction started."),
            ["base.done"] = ("Tamamlandı.", "Completed."),

            ["building.core_reactor"] = ("Çekirdek Reaktör", "Core Reactor"),
            ["building.oxygen_plant"] = ("Oksijen Tesisi", "Oxygen Plant"),
            ["building.food_lab"] = ("Gıda Laboratuvarı", "Food Lab"),
            ["building.bio_lab"] = ("Biyo Laboratuvar", "Bio Lab"),
            ["building.research_center"] = ("Araştırma Merkezi", "Research Center"),
            ["building.drone_factory"] = ("Dron Fabrikası", "Drone Factory"),
            ["building.submarine_dock"] = ("Denizaltı İskelesi", "Submarine Dock"),
            ["building.military_dome"] = ("Askeri Kubbe", "Military Dome"),
            ["building.shield_generator"] = ("Kalkan Jeneratörü", "Shield Generator"),
            ["building.radar_center"] = ("Radar Merkezi", "Radar Center"),
            ["building.mega_dome"] = ("Mega Kubbe", "Mega Dome"),
            ["building.alliance_hub"] = ("İttifak Merkezi", "Alliance Hub"),
            ["building.decoration_spire"] = ("Süs Kulesi", "Decoration Spire"),

            ["desc.core_reactor"] = ("Üssün kalbi. Diğer binalar reaktör seviyesini geçemez.", "Heart of the base. No building can outlevel the reactor."),
            ["desc.oxygen_plant"] = ("Oksijen üretir. Oksijen biterse üs ölür.", "Produces oxygen. Without oxygen the base dies."),
            ["desc.food_lab"] = ("Mürettebatı besler.", "Feeds the crew."),
            ["desc.bio_lab"] = ("Canlı organizmalardan biyokütle üretir.", "Grows biomass from living organisms."),
            ["desc.research_center"] = ("Yeni teknolojilerin kapısı.", "Gateway to new technology."),
            ["desc.drone_factory"] = ("Kurtarma dronları enkazdan titanyum toplar.", "Salvage drones recover titanium from wrecks."),
            ["desc.submarine_dock"] = ("Denizaltı inşa eder ve keşfe gönderir.", "Builds submarines and launches expeditions."),
            ["desc.military_dome"] = ("Üs savunmasını eğitir.", "Trains the base's defenders."),
            ["desc.shield_generator"] = ("Basınca ve saldırılara karşı kalkan.", "Shields against pressure and attacks."),
            ["desc.radar_center"] = ("Yaklaşan tehditleri görür.", "Detects incoming threats."),
            ["desc.mega_dome"] = ("Üssü yeni bir kubbeye genişletir.", "Expands the base into a new dome."),
            ["desc.alliance_hub"] = ("İttifak bağlantısı.", "Alliance link."),
            ["desc.decoration_spire"] = ("Üssünü süsler.", "Decorates your base."),

            ["resource.Oxygen"] = ("Oksijen", "Oxygen"),
            ["resource.Energy"] = ("Enerji", "Energy"),
            ["resource.Food"] = ("Gıda", "Food"),
            ["resource.Biomass"] = ("Biyokütle", "Biomass"),
            ["resource.Titanium"] = ("Titanyum", "Titanium"),
            ["resource.Hadalite"] = ("Hadalit", "Hadalite"),

            ["reason.BuilderBusy"] = ("İnşaatçı meşgul: aynı anda tek inşaat.", "Builder busy: one construction at a time."),
            ["reason.InsufficientResources"] = ("Kaynak yetersiz.", "Not enough resources."),
            ["reason.ReactorLevelTooLow"] = ("Önce Çekirdek Reaktörü yükselt.", "Upgrade the Core Reactor first."),
            ["reason.MaxLevel"] = ("En yüksek seviyede.", "Already at max level."),
            ["reason.UnderConstruction"] = ("Zaten inşa ediliyor.", "Already under construction."),
            ["reason.NotYetFree"] = ("Ücretsiz bitirmek için son 5 dakikayı bekle.", "Free finish opens in the last 5 minutes."),
            ["reason.NotUnderConstruction"] = ("İnşaat yok.", "Nothing is being built."),
            ["reason.SlotOccupied"] = ("Bu yuva dolu.", "This slot is taken."),
            ["reason.RingLocked"] = ("Bu halka henüz açılmadı.", "This ring is still locked."),
            ["reason.SectorLocked"] = ("Bu bölüm henüz açılmadı.", "This sector is still locked."),
            ["reason.RingNotAllowed"] = ("Bu bina bu halkaya kurulamaz.", "This building does not fit this ring."),
            ["reason.SlotTypeMismatch"] = ("Bu yuva bu bina için değil.", "This slot is not for this building."),
            ["reason.SectorRestricted"] = ("Bu yuva başka bir bina için ayrılmış.", "This slot is reserved for another building."),
            ["reason.UnknownBuilding"] = ("Bina bulunamadı.", "Building not found."),
            ["reason.NotSupported"] = ("Bu işlem henüz desteklenmiyor.", "Not supported yet."),

            ["error.NameInvalid"] = ("Ad 3–16 karakter olmalı; harf, rakam, boşluk, _ ve - kullanılabilir.", "Names are 3–16 characters: letters, digits, space, _ and -."),
            ["error.PasswordInvalid"] = ("Şifre en az 8 karakter olmalı.", "The password needs at least 8 characters."),
            ["error.PasswordMismatch"] = ("Şifreler eşleşmiyor.", "The passwords do not match."),
            ["error.InvalidCredentials"] = ("Kod ya da şifre hatalı.", "Wrong code or password."),
            ["error.RateLimited"] = ("Çok fazla deneme. Biraz sonra tekrar dene.", "Too many attempts. Try again later."),
            ["error.Network"] = ("Sunucuya ulaşılamadı.", "Could not reach the server."),
            ["error.generic"] = ("Bir şeyler ters gitti ({0}).", "Something went wrong ({0})."),
        };

        public static bool Turkish { get; set; } = Application.systemLanguage == SystemLanguage.Turkish;

        public static string T(string key)
        {
            if (!Strings.TryGetValue(key, out var value))
                return key;

            return Turkish ? value.Tr : value.En;
        }

        /// <summary>Player-facing text for a server reject reason (PlacementRejectReason / ConstructionRejectReason).</summary>
        public static string Reason(string code) =>
            Strings.ContainsKey("reason." + code) ? T("reason." + code) : string.Format(T("error.generic"), code);

        public static string Building(string id) => Strings.ContainsKey("building." + id) ? T("building." + id) : id;

        /// <summary>m:ss under an hour, h:mm:ss above.</summary>
        public static string Duration(float seconds)
        {
            var t = System.TimeSpan.FromSeconds(Mathf.Max(0f, Mathf.Ceil(seconds)));
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
        }

        public static string Error(string code) =>
            Strings.ContainsKey("error." + code) ? T("error." + code) : string.Format(T("error.generic"), code);
    }
}
