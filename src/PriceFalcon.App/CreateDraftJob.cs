using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class CreateDraftJob : IRequest<string?>
    {
        public string Token { get; set; } = string.Empty;

        public Uri Website { get; set; } = new Uri("about:blank");
    }

    internal class CreateDraftJobHandler : IRequestHandler<CreateDraftJob, string?>
    {
        private readonly ITokenService _tokenService;
        private readonly IDraftJobRepository _draftJobRepository;

        public CreateDraftJobHandler(ITokenService tokenService, IDraftJobRepository draftJobRepository)
        {
            _tokenService = tokenService;
            _draftJobRepository = draftJobRepository;
        }

        public async Task<string?> Handle(CreateDraftJob request, CancellationToken cancellationToken)
        {
            var tokenValidation = await _tokenService.ValidateToken(request.Token, Token.TokenPurpose.CreateJob);

            if (tokenValidation.Status != TokenValidationStatus.Success)
            {
                return null;
            }

            await _tokenService.Revoke(request.Token);

            var monitoringToken = await _tokenService.GenerateToken(tokenValidation.UserId!.Value, Token.TokenPurpose.MonitorJob, DateTime.UtcNow.AddDays(10));

            var job = await _draftJobRepository.Create(request.Website, tokenValidation.UserId!.Value);

            await _draftJobRepository.SetMonitoringTokenId(job.Id, monitoringToken.id);

            return monitoringToken.token;
        }
    }
}
