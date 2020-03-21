using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Claudia.Services{
    /**
     * <summary>A generator service: generates passwords, ids etc.</summary>
     * 
     */
    public class HashGeneratorService : IGenerator
    {
        private static KeyDerivationPrf Prf { get; set; } = KeyDerivationPrf.HMACSHA256;
        
        public string GenerateId(string aboslutPath)
        {
            return Hash(aboslutPath);
        }

        public void SetDerivationPrf(KeyDerivationPrf prf)
        {
            Prf = prf;
        }

        public void Dispose()
        {
            
        }

        public string GeneratePassword()
        {
            var gbase = Guid.NewGuid().ToString();
            return Hash(gbase);
        }


        #region Helpers
        private string Hash(string input)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: input,
                salt: salt,
                prf: Prf,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            PrepareHashString(ref hash);
            return hash;
        }

        private void PrepareHashString(ref string hash)
        {
            var dictionary = new Dictionary<char, char>
            {
                { '+', '_' },
                { '=', '-' },
                { '\\', '$' },
                { '/', '.' }
            };

            foreach (var c in hash.ToCharArray())
            {
                if (!dictionary.ContainsKey(c)) continue;
                dictionary.TryGetValue(c, out char replaceChar);
                hash = hash.Replace(c, replaceChar);
            }

        }
        #endregion

    }
}