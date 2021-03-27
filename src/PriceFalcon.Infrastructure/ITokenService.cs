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
        Task<(int id, string token)> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc);

        Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose);

        Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose);

        Task Revoke(string token);

        Task<string?> GetById(int id);
    }

    internal class TokenService : ITokenService
    {
        private readonly ITokenRepository _tokenRepository;

        public TokenService(ITokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public async Task<(int id, string token)> GenerateToken(int userId, Token.TokenPurpose purpose, DateTime expiryUtc)
        {
            using var rng = new RNGCryptoServiceProvider();

            var tokenBuffer = new byte[64];
            rng.GetBytes(tokenBuffer);

            // Replace the URL unsafe characters for our own rubbish encoding scheme.
            var value = Convert.ToBase64String(tokenBuffer).Replace('/', '_')
                .Replace('+', '-').TrimEnd('=');

            var entity =  await _tokenRepository.Create(value, userId, purpose, expiryUtc);

            return (entity.Id, entity.Value);
        }

        public async Task<TokenValidationResult> ValidateToken(string token, Token.TokenPurpose purpose)
        {
            var tokenStored = await _tokenRepository.GetByValue(token);

            if (tokenStored == null)
            {
                return new TokenValidationResult(TokenValidationStatus.Invalid, null);
            }

            if (DateTime.UtcNow > tokenStored.Expiry || tokenStored.IsUsed)
            {
                return new TokenValidationResult(TokenValidationStatus.Expired, tokenStored.UserId);
            }

            return new TokenValidationResult(TokenValidationStatus.Success, tokenStored.UserId);
        }

        public async Task<Token?> GetLastToken(int userId, Token.TokenPurpose purpose)
        {
            return await _tokenRepository.GetLastToken(userId, purpose);
        }

        public async Task Revoke(string token)
        {
            await _tokenRepository.Revoke(token);
        }

        public async Task<string?> GetById(int id)
        {
            return (await _tokenRepository.GetById(id))?.Value;
        }
    }

    public enum TokenValidationStatus
    {
        Success = 1,
        Expired = 2,
        Invalid = 3
    }
}
