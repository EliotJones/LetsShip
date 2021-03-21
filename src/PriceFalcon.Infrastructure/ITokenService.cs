using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Infrastructure
{
    public class TokenValidationResult
    {
        public TokenValidationStatus Status { get; }

        public int? UserId { get; }

        public TokenValidationResult(TokenValidationStatus status, int? userId)
        {
            Status = status;
            UserId = userId;
        }
    }

    public interface ITokenService
    {
        Task<string> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc);

        Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose);

        Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose);
    }

    internal class TokenService : ITokenService
    {
        private readonly ITokenRepository _tokenRepository;

        public TokenService(ITokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public async Task<string> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc)
        {
            using var rng = new RNGCryptoServiceProvider();

            var tokenBuffer = new byte[64];
            rng.GetBytes(tokenBuffer);

            var value = Convert.ToBase64String(tokenBuffer);

            await _tokenRepository.Create(value, userId, purpose, expiryUtc);

            return value;
        }

        public async Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose)
        {
            var tokenStored = await _tokenRepository.GetByValue(token);

            if (tokenStored == null)
            {
                return new TokenValidationResult(TokenValidationStatus.Invalid, null);
            }

            if (DateTime.UtcNow > tokenStored.Expiry)
            {
                return new TokenValidationResult(TokenValidationStatus.Expired, tokenStored.UserId);
            }

            return new TokenValidationResult(TokenValidationStatus.Success, tokenStored.UserId);
        }

        public async Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose)
        {
            return await _tokenRepository.GetLastToken(userId, purpose);
        }
    }

    public enum TokenValidationStatus
    {
        Success = 1,
        Expired = 2,
        Invalid = 3
    }
}
