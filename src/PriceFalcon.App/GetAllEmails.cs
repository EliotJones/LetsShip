using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;

namespace PriceFalcon.App
{
    public class GetAllEmails : IRequest<IReadOnlyList<Email>>
    {
    }

    internal class GetAllEmailsHandler : IRequestHandler<GetAllEmails, IReadOnlyList<Email>>
    {
        private readonly IEmailService _emailService;

        public GetAllEmailsHandler(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<IReadOnlyList<Email>> Handle(GetAllEmails request, CancellationToken cancellationToken)
        {
            return await _emailService.GetAllSent();
        }
    }
}
