using FraudGuard.Domain.Interfaces.Abstractions;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FraudGuard.Infrastructure.Services
{
    public class CryptService : ICryptService
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100000;

        public string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            
            // Hash the password using PBKDF2 static method
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );
            
            // Format as $PBKDF2$iterations$salt$hash
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword)) return false;

            // Fallback: If it's a legacy SHA256 hash (does not start with PBKDF2$)
            if (!hashedPassword.StartsWith("PBKDF2$"))
            {
                using var sha256 = SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                string legacyHash = Convert.ToBase64String(hashedBytes);
                return legacyHash == hashedPassword;
            }

            // Parse PBKDF2 format
            var parts = hashedPassword.Split('$');
            if (parts.Length != 5) return false;

            try
            {
                int iterations = int.Parse(parts[2]);
                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] hash = Convert.FromBase64String(parts[4]);

                // Hash input using same parameters
                byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    hash.Length
                );

                // Constant-time comparison
                return CryptographicOperations.FixedTimeEquals(hash, newHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
