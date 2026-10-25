using System.Security.Cryptography;
using System.Text;

namespace Hadal.Gateway.Auth
{
    /// <summary>PBKDF2-SHA256 password hashes ("pbkdf2$iterations$salt$hash", base64 parts).</summary>
    public static class PasswordHasher
    {
        private const int Iterations = 210_000; // OWASP 2023 recommendation for PBKDF2-SHA256
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
            return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2" || !int.TryParse(parts[1], out var iterations))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public static class TransferCodes
    {
        // No 0/O, 1/I/L: codes are read off one screen and typed into another.
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int Length = 12;

        /// <summary>A fresh code in stored form (12 characters, no dashes, ~59 bits).</summary>
        public static string New()
        {
            Span<char> code = stackalloc char[Length];
            for (var i = 0; i < Length; i++)
                code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            return new string(code);
        }

        /// <summary>Display form: XXXX-XXXX-XXXX.</summary>
        public static string Format(string code) =>
            code.Length == Length ? $"{code[..4]}-{code[4..8]}-{code[8..]}" : code;
    }
}
