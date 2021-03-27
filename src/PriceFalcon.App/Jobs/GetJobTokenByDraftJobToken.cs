using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Jobs
{
    public class GetJobTokenByDraftJobToken : IRequest<string?>
    {
        public string DraftJobToken { get; }

        public GetJobTokenByDraftJobToken(string draftJobToken)
        {
            DraftJobToken = draftJobToken;
        }
    }

    internal class GetJobTokenByDraftJobTokenHandler : IRequestHandler<GetJobTokenByDraftJobToken, string?>
    {
        private readonly ITokenService _tokenService;
        private readonly IJobRepository _jobRepository;
        private readonly IDraftJobRepository _draftJobRepository;

        public GetJobTokenByDraftJobTokenHandler(ITokenService tokenService, IJobRepository jobRepository, IDraftJobRepository draftJobRepository)
        {
            _tokenService = tokenService;
            _jobRepository = jobRepository;
            _draftJobRepository = draftJobRepository;
        }

        public async Task<string?> Handle(GetJobTokenByDraftJobToken request, CancellationToken cancellationToken)
        {
            var draftJob = await _draftJobRepository.GetByMonitoringToken(request.DraftJobToken);

            if (draftJob == null)
            {
                return null;
            }

            var job = await _jobRepository.GetByDraftJobId(draftJob.Id);

            if (job == null)
            {
                return null;
            }

            return await _tokenService.GetById(job.TokenId);
        }
    }
}
