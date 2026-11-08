using System.Security.Cryptography;
using System.Text;
using CineAboutApp.Interfaces.General;
using Konscious.Security.Cryptography;

namespace CineAboutApp.Services.General
{
    public class Argon2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int DegreeOfParallelism = 2;
        private const int MemorySize = 65536;
        private const int Iterations = 4;
        private const int HashSize = 32;

        public (string Hash, string Salt) Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hashBytes = ComputeHash(password, salt);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(salt));
        }

        public bool Verify(string hash, string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            var hashedPlain = ComputeHash(password, Convert.FromBase64String(salt));
            return CryptographicOperations.FixedTimeEquals(hashedPlain, Convert.FromBase64String(hash));
        }

        private static byte[] ComputeHash(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations
            };

            return argon2.GetBytes(HashSize);
        }
    }
}
