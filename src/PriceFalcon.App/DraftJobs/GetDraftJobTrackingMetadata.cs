using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.DraftJobs
{
    public class DraftJobTrackingMetadata
    {
        public Uri Url { get; set; } = new Uri("about:blank");

        public string UserEmail { get; set; } = string.Empty;
    }

    public class GetDraftJobTrackingMetadata : IRequest<DraftJobTrackingMetadata?>
    {
        public string Token { get; }

        public GetDraftJobTrackingMetadata(string token)
        {
            Token = token;
        }
    }

    internal class GetDraftJobTrackingMetadataHandler : IRequestHandler<GetDraftJobTrackingMetadata, DraftJobTrackingMetadata?>
    {
        private readonly IDraftJobRepository _draftJobRepository;
        private readonly IUserRepository _userRepository;

        public GetDraftJobTrackingMetadataHandler(IDraftJobRepository draftJobRepository, IUserRepository userRepository)
        {
            _draftJobRepository = draftJobRepository;
            _userRepository = userRepository;
        }

        public async Task<DraftJobTrackingMetadata?> Handle(GetDraftJobTrackingMetadata request, CancellationToken cancellationToken)
        {
            var draftJob = await _draftJobRepository.GetByMonitoringToken(request.Token);

            if (draftJob == null)
            {
                return null;
            }

            var user = await _userRepository.GetById(draftJob.UserId);

            if (user == null)
            {
                return null;
            }

            return new DraftJobTrackingMetadata
            {
                UserEmail = user.Email,
                Url = draftJob.Url
            };
        }
    }
}
