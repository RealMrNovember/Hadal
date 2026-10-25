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

        public static string Error(string code) =>
            Strings.ContainsKey("error." + code) ? T("error." + code) : string.Format(T("error.generic"), code);
    }
}
