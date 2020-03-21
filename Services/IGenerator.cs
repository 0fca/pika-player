using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Claudia.Services{
    /**
     * <summary>Base interface for GeneratorService used to register service in DI.</summary>
     */
    public interface IGenerator
    {
        string GenerateId(string aboslutPath);
        string GeneratePassword();
        void SetDerivationPrf(KeyDerivationPrf prf);
        void Dispose();
    }
}