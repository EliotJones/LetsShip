using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PriceFalcon.Domain;

namespace PriceFalcon.Infrastructure
{
    public interface ITokenService
    {
        Task<string> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc);

        Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose);

        Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose);
    }

    internal class TokenService : ITokenService
    {
        public Task<string> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc)
        {
            using var rng = new RNGCryptoServiceProvider();

            var tokenBuffer = new byte[64];
            rng.GetBytes(tokenBuffer);

            return Task.FromResult(Convert.ToBase64String(tokenBuffer));
        }

        public Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose)
        {
            throw new NotImplementedException();
        }

        public Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose)
        {
            throw new NotImplementedException();
        }
    }

    public enum TokenValidationResult
    {
        Success = 1,
        Expired = 2,
        Invalid = 3
    }
}
