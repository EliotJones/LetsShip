using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Jobs
{
    public class CancelJob : IRequest
    {
        public string Token { get; }

        public CancelJob(string token)
        {
            Token = token;
        }
    }

    internal class CancelJobHandler : IRequestHandler<CancelJob>
    {
        private readonly IJobRepository _jobRepository;

        public CancelJobHandler(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<Unit> Handle(CancelJob request, CancellationToken cancellationToken)
        {
            var job = await _jobRepository.GetByToken(request.Token);

            await _jobRepository.CancelJob(job.Id);

            return Unit.Value;
        }
    }
}