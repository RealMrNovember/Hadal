using System.Security.Cryptography;
using System.Text;

namespace Hadal.Gateway.Admin
{
    /// <summary>RFC 6238 time-based one-time passwords (Google Authenticator compatible: SHA-1, 6 digits, 30 s).</summary>
    public static class Totp
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string NewSecret() => Base32(RandomNumberGenerator.GetBytes(20));

        public static string ProvisioningUri(string account, string secret) =>
            $"otpauth://totp/HADAL%20Overseer:{Uri.EscapeDataString(account)}?secret={secret}&issuer=HADAL%20Overseer&digits=6&period=30";

        /// <summary>Accepts the current step and one step either side (clock drift).</summary>
        public static bool Verify(string secret, string code, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(secret) || code == null || code.Length != 6 || !code.All(char.IsDigit))
                return false;

            var key = FromBase32(secret);
            var step = now.ToUnixTimeSeconds() / 30;
            for (var offset = -1; offset <= 1; offset++)
            {
                if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(key, step + offset)), Encoding.ASCII.GetBytes(code)))
                    return true;
            }

            return false;
        }

        public static string Code(byte[] key, long step)
        {
            var counter = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);
            var hash = HMACSHA1.HashData(key, counter);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            return (binary % 1_000_000).ToString("D6");
        }

        public static string Base32(byte[] data)
        {
            var output = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
                output.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return output.ToString();
        }

        public static byte[] FromBase32(string text)
        {
            var bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (var c in text.TrimEnd('=').ToUpperInvariant())
            {
                var value = Alphabet.IndexOf(c);
                if (value < 0)
                    continue;
                buffer = (buffer << 5) | value;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }

            return bytes.ToArray();
        }
    }
}
