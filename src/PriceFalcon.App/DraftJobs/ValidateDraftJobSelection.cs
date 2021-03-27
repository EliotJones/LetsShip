using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Crawler;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class DraftJobSelectionValidationResponse
    {
        public bool IsValid { get; set; }

        public decimal? Price { get; set; }

        public string? Reason { get; set; }
    }

    public class ValidateDraftJobSelection : IRequest<DraftJobSelectionValidationResponse>
    {
        public string Token { get; }

        public HtmlElementSelection Selection { get; }

        public ValidateDraftJobSelection(string token, HtmlElementSelection selection)
        {
            Token = token;
            Selection = selection;
        }
    }

    internal class ValidateDraftJobSelectionHandler : IRequestHandler<ValidateDraftJobSelection, DraftJobSelectionValidationResponse>
    {
        private readonly IDraftJobRepository _draftJobRepository;

        public ValidateDraftJobSelectionHandler(IDraftJobRepository draftJobRepository)
        {
            _draftJobRepository = draftJobRepository;
        }

        public async Task<DraftJobSelectionValidationResponse> Handle(ValidateDraftJobSelection request, CancellationToken cancellationToken)
        {
            var job = await _draftJobRepository.GetByMonitoringToken(request.Token);

            if (job == null)
            {
                return new DraftJobSelectionValidationResponse
                {
                    IsValid = false,
                    Reason = "No job exists with a matching token"
                };
            }

            if (string.IsNullOrWhiteSpace(job.CrawledHtml))
            {
                return new DraftJobSelectionValidationResponse
                {
                    IsValid = false,
                    Reason = "We haven't retrieved the HTML for this site yet."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Selection.Text))
            {
                return new DraftJobSelectionValidationResponse
                {
                    IsValid = false,
                    Reason = "No text in the element."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Selection.Element))
            {
                return new DraftJobSelectionValidationResponse
                {
                    IsValid = false,
                    Reason = "No element was selected"
                };
            }

            if (!PriceCrawlValidator.TryGetPrice(request.Selection.Text, out var price))
            {
                return new DraftJobSelectionValidationResponse
                {
                    IsValid = false,
                    Reason = $"No price could be identified in the text: {request.Selection.Text}"
                };
            }

            if (!XPathCalculator.TryGetXPath(job.CrawledHtml, request.Selection, out _))
            {
                return new DraftJobSelectionValidationResponse
                {
                    Price = price,
                    IsValid = false,
                    Reason = "Could not uniquely locate the element to track, sorry for the inconvenienc."
                };
            }

            return new DraftJobSelectionValidationResponse
            {
                Price = price,
                IsValid = true,
                Reason = "Value found successfully"
            };
        }
    }
}
