using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Jobs
{
    public class NotifyJobListeners : IRequest
    {
    }

    internal class NotifyJobListenersHandler : IRequestHandler<NotifyJobListeners>
    {
        private readonly IEmailService _emailService;
        private readonly IJobRepository _jobRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly PriceFalconConfig _config;

        public NotifyJobListenersHandler(IEmailService emailService,
            IJobRepository jobRepository,
            IUserRepository userRepository,
            ITokenService tokenService,
            PriceFalconConfig config)
        {
            _emailService = emailService;
            _jobRepository = jobRepository;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _config = config;
        }

        public async Task<Unit> Handle(NotifyJobListeners request, CancellationToken cancellationToken)
        {
            var count = await _emailService.GetSentTodayCount();

            if (count >= 100)
            {
                return Unit.Value;
            }

            var jobIdsWithUnnotifiedRuns = await _jobRepository.GetJobIdsWithRunsNotNotified();

            foreach (var jobId in jobIdsWithUnnotifiedRuns)
            {
                count = await _emailService.GetSentTodayCount();

                if (count >= 100)
                {
                    return Unit.Value;
                }

                var job = await _jobRepository.GetById(jobId);
                var user = await _userRepository.GetById(job.UserId);
                var token = await _tokenService.GetById(job.TokenId);

                if (!job.StartPrice.HasValue || user == null || string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                var runs = await _jobRepository.GetJobRunsByJobId(jobId);

                if (runs.Count == 0)
                {
                    continue;
                }

                var latest = runs[0];
                var uri = new Uri(_config.SiteUrl, $"/jobs/{token}");

                if (latest.Status == JobRunStatus.Succeeded && latest.Price.HasValue
                && Math.Abs(job.StartPrice.Value - latest.Price.Value) >= 1)
                {
                    var changeType = latest.Price.Value > job.StartPrice.Value
                        ? "Unfortunately the price has increased and is now"
                        : "It's your lucky day, the price has decreased to";

                    var body = $@"<p>Hi there,</p>
                        <p>You asked us to let you know if the original price ({job.StartPrice}) for the item at {job.Url} changed.</p>
                        <p>{changeType} {latest.Price.Value.ToString("N2", CultureInfo.InvariantCulture)}.</p>
                        <p>You can review this price watch job here: <a href='{uri}'>Job Link</a>.</p>";

                    var outcome = await _emailService.Send(user.Email, $"Price Change for {job.Url}", body);

                    if (outcome != EmailSendResult.Success)
                    {
                        continue;
                    }
                }
                else
                {
                    var failCount = 0;
                    foreach (var run in runs)
                    {
                        if (run.Status != JobRunStatus.Failed)
                        {
                            break;
                        }

                        failCount++;

                        if (run.IsNotified)
                        {
                            // Don't re-notify, this would just be spam.
                            failCount = 0;
                            break;
                        }
                    }

                    if (failCount >= 2)
                    {
                        var body = $@"<p>Hi there,</p>
                            <p>Unfortunately the past {failCount} attempts to monitor the price at {job.Url} failed.</p>
                            <p>This can happen when the website changes its design and the original element to monitor moves.</p>
                            <p>You can review and cancel this price watch job here: <a href='{uri}'>Job Link</a>.</p>";

                        await _emailService.Send(user.Email, $"Price Watch failure for {job.Url}", body);
                    }
                }

                await _jobRepository.MarkAllJobRunsNotifiedForJob(job.Id);
            }

            return Unit.Value;
        }
    }
}
