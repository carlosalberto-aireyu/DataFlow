
using System.Security.Cryptography;
using System.Text;

namespace DataFlow.UI.Services
{
    public static class AlphaVersionService
    {
        public const string VERSION = "1.0.0-rc1";
        private static readonly DateTime EXPIRY_DATE = new DateTime(2026, 3, 31);
        private const string SECURITY_HASH = "5F16E3";

        public static bool IsExpired => DateTime.Now > EXPIRY_DATE;

        public static bool IsValid
        {
            get
            {
                if (IsExpired)
                    return false;
                var hash = GenerateHash(EXPIRY_DATE.ToString("yyyyMMdd"));
                return hash.StartsWith(SECURITY_HASH);
            }
        }

        public static string GetExpiryMessage()
        {
            if(IsExpired)
                return $"La versión Release Candidate (RC) de esta aplicación expiró el {EXPIRY_DATE:dd/MM/yyyy}.\nContacte al administrador para obtener una versión actualizada.";
            var daysLeft = (EXPIRY_DATE - DateTime.Now).Days;
            return $"Versión Release Candidate (RC) - Expira en {daysLeft} días ({EXPIRY_DATE:dd/MM/yyyy})";
        }
        public static string GetVersionInfo()
        {
            return $"DataFlow v{VERSION}";
        }
        private static string GenerateHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToHexString(hashedBytes);
            }
        }
    }
}
