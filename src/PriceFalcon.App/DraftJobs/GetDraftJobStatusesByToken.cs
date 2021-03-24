using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class GetDraftJobStatusesByToken : IRequest<IReadOnlyList<DraftJobLog>>
    {
        public string Token { get; }

        public GetDraftJobStatusesByToken(string token)
        {
            Token = token;
        }
    }

    internal class GetDraftJobStatusesByTokenHandler : IRequestHandler<GetDraftJobStatusesByToken, IReadOnlyList<DraftJobLog>>
    {
        private readonly IDraftJobRepository _draftJobRepository;

        public GetDraftJobStatusesByTokenHandler(IDraftJobRepository draftJobRepository)
        {
            _draftJobRepository = draftJobRepository;
        }

        public async Task<IReadOnlyList<DraftJobLog>> Handle(GetDraftJobStatusesByToken request, CancellationToken cancellationToken)
        {
            var job = await _draftJobRepository.GetByMonitoringToken(request.Token);

            if (job == null)
            {
                return Array.Empty<DraftJobLog>();
            }

            return await _draftJobRepository.GetLogs(job.Id);
        }
    }
}
