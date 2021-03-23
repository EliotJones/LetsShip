using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceFalcon.Crawler;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.JobRunner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDraftJobRepository _draftJobRepository;
        private readonly ICrawler _crawler;

        private readonly List<Task> _running = new List<Task>();

        public Worker(ILogger<Worker> logger, IDraftJobRepository draftJobRepository, ICrawler crawler)
        {
            _logger = logger;
            _draftJobRepository = draftJobRepository;
            _crawler = crawler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var pending = await _draftJobRepository.GetJobsInStatus(DraftJobStatus.Pending);

                foreach (var draftJob in pending)
                {
                    var task = Task.Run(
                            async () =>
                            {
                                await RunDraftJob(draftJob, stoppingToken);
                            },
                            stoppingToken)
                        .ContinueWith(t => _running.Remove(t), stoppingToken);

                    _running.Add(task);
                }

                await Task.Delay(3000, stoppingToken);
            }
        }

        private async Task RunDraftJob(DraftJob job, CancellationToken token)
        {
            try
            {
                using var jobLock = await _draftJobRepository.AcquireJobLock(job.Id);

                if (jobLock.Status != DraftJobStatus.Pending)
                {
                    jobLock.Abandon();
                    return;
                }

                await jobLock.Log("Job is queued, it will run soon.", DraftJobStatus.Queued);

                await jobLock.SetStatus(DraftJobStatus.Processing);

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    async Task LoggerMethod(string s) => await jobLock.Log(s, DraftJobStatus.Processing);

                    var pageSource = await _crawler.GetPageSource(job.Url, LoggerMethod, token);

                    await jobLock.SetHtml(pageSource);

                    await jobLock.SetStatus(DraftJobStatus.Completed);

                    jobLock.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed crawl for {job.Url} due to an error.");

                    jobLock.Abandon();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"Failed to acquire job lock for job {job.Id}.");
                // ignored
            }
        }
    }
}
