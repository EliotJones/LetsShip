using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Crawler;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Jobs
{
    public class CreateJobResult
    {
        public ResultStatus Status { get; set; }

        public string? Token { get; set; } = string.Empty;

        public enum ResultStatus
        {
            DraftNotFound = 0,
            Created = 1,
            LimitReached = 2,
            SelectionInvalid = 3,
            AlreadyExists = 4
        }
    }

    public class CreateJob : IRequest<CreateJobResult>
    {
        public string DraftJobToken { get; set; } = string.Empty;

        public HtmlElementSelection Selector { get; set; } = new HtmlElementSelection();
    }

    internal class CreateJobHandler : IRequestHandler<CreateJob, CreateJobResult>
    {
        private readonly ITokenService _tokenService;
        private readonly IDraftJobRepository _draftJobRepository;
        private readonly IJobRepository _jobRepository;

        public CreateJobHandler(ITokenService tokenService,
            IDraftJobRepository draftJobRepository,
            IJobRepository jobRepository)
        {
            _tokenService = tokenService;
            _draftJobRepository = draftJobRepository;
            _jobRepository = jobRepository;
        }

        public async Task<CreateJobResult> Handle(CreateJob request, CancellationToken cancellationToken)
        {
            var draftJob = await _draftJobRepository.GetByMonitoringToken(request.DraftJobToken);

            if (draftJob == null || string.IsNullOrWhiteSpace(draftJob.CrawledHtml))
            {
                return new CreateJobResult
                {
                    Status = CreateJobResult.ResultStatus.DraftNotFound
                };
            }

            var existing = await _jobRepository.GetByDraftJobId(draftJob.Id);
            if (existing != null)
            {
                return new CreateJobResult
                {
                    Status = CreateJobResult.ResultStatus.AlreadyExists,
                    Token = await _tokenService.GetById(existing.TokenId)
                };
            }

            var jobCount = await _jobRepository.GetJobCountForUser(draftJob.UserId);
            if (jobCount >= 3)
            {
                return new CreateJobResult
                {
                    Status = CreateJobResult.ResultStatus.LimitReached
                };
            }

            if (!PriceCrawlValidator.TryGetPrice(request.Selector.Text, out var price)
                || !XPathCalculator.TryGetXPath(draftJob.CrawledHtml, request.Selector, out var xpath))
            {
                return new CreateJobResult
                {
                    Status = CreateJobResult.ResultStatus.SelectionInvalid
                };
            }

            var jobToken = await _tokenService.GenerateToken(draftJob.UserId, Token.TokenPurpose.Job,
                // haha
                DateTime.UtcNow.AddYears(20));

            await _jobRepository.Create(jobToken.id, request.Selector, price, xpath, draftJob.Id);

            return new CreateJobResult
            {
                Status = CreateJobResult.ResultStatus.Created,
                Token = jobToken.token
            };
        }
    }
}
