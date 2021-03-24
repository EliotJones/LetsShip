using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class GetDraftJobHtmlByToken : IRequest<string>
    {
        public string Token { get; }

        public GetDraftJobHtmlByToken(string token)
        {
            Token = token;
        }
    }

    internal class GetDraftJobHtmlByTokenHandler : IRequestHandler<GetDraftJobHtmlByToken, string>
    {
        private readonly IDraftJobRepository _draftJobRepository;

        public GetDraftJobHtmlByTokenHandler(IDraftJobRepository draftJobRepository)
        {
            _draftJobRepository = draftJobRepository;
        }

        public async Task<string> Handle(GetDraftJobHtmlByToken request, CancellationToken cancellationToken)
        {
            var job = await _draftJobRepository.GetByMonitoringToken(request.Token);

            return job?.CrawledHtml ?? string.Empty;
        }
    }
}
