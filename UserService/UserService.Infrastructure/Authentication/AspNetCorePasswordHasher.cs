using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UserService.Application.Interfaces.Services;

namespace UserService.Infrastructure.Authentication
{
    // Simple, secure PBKDF2 password hasher. Produces hashes in the format: iterations.salt.base64hash
    public class AspNetCorePasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100_000;

        public Task<string> HashPasswordAsync(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            var saltBase64 = Convert.ToBase64String(salt);
            var keyBase64 = Convert.ToBase64String(key);
            var hash = $"{Iterations}.{saltBase64}.{keyBase64}";
            return Task.FromResult(hash);
        }

        public Task<bool> VerifyHashedPasswordAsync(string hashedPassword, string providedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.', 3);
                if (parts.Length != 3) return Task.FromResult(false);
                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(providedPassword, salt, iterations, HashAlgorithmName.SHA256);
                var keyToCheck = pbkdf2.GetBytes(key.Length);

                var verified = CryptographicOperations.FixedTimeEquals(key, keyToCheck);
                return Task.FromResult(verified);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
