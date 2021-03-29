using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
        private readonly IJobRepository _jobRepository;
        private readonly ICrawler _crawler;

        private readonly List<Task> _running = new List<Task>();

        public Worker(ILogger<Worker> logger, IDraftJobRepository draftJobRepository, IJobRepository jobRepository, ICrawler crawler)
        {
            _logger = logger;
            _draftJobRepository = draftJobRepository;
            _jobRepository = jobRepository;
            _crawler = crawler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var pending = await _draftJobRepository.GetJobsInStatus(DraftJobStatus.Pending);

                _logger.LogInformation($"{pending.Count} pending draft jobs found.");

                foreach (var draftJob in pending)
                {
                    if (_running.Count >= 5)
                    {
                        _logger.LogError($"Too many running tasks were found {_running.Count}, skipping pending draft jobs.");
                        continue;
                    }

                    var task = Task.Run(
                            async () =>
                            {
                                await RunDraftJob(draftJob, stoppingToken);
                            },
                            stoppingToken)
                        .ContinueWith(t => _running.Remove(t), stoppingToken);

                    _running.Add(task);
                }

                var pendingJobs = await _jobRepository.GetJobsDue();

                _logger.LogInformation($"{pending.Count} pending jobs found.");

                foreach (var job in pendingJobs)
                {
                    if (_running.Count >= 5)
                    {
                        _logger.LogError($"Too many running tasks were found {_running.Count}, skipping pending jobs.");
                        continue;
                    }

                    var task = Task.Run(
                            async () =>
                            {
                                await RunJob(job, stoppingToken);
                            },
                            stoppingToken)
                        .ContinueWith(t => _running.Remove(t), stoppingToken);

                    _running.Add(task);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Disposing of crawler.");

            try
            {
                _crawler.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing of crawler.");
            }

            return base.StopAsync(cancellationToken);
        }

        private async Task RunDraftJob(DraftJob job, CancellationToken token)
        {
            try
            {
                _logger.LogInformation($"Trying to acquire lock for draft job {job.Id}.");

                using var jobLock = await _draftJobRepository.AcquireJobLock(job.Id, token);

                if (jobLock.Status != DraftJobStatus.Pending)
                {
                    jobLock.Abandon();
                    return;
                }

                _logger.LogInformation($"Acquired lock, beginning draft job {job.Id}.");

                await jobLock.Log("Job is queued, it will run soon.", DraftJobStatus.Queued);

                await jobLock.SetStatus(DraftJobStatus.Processing);

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    async Task LoggerMethod(string s) => await jobLock.Log(s, DraftJobStatus.Processing);

                    var pageSource = await _crawler.GetPageSource(job.Url, LoggerMethod, token);

                    await jobLock.SetHtml(pageSource);

                    await jobLock.Log("Job completed", DraftJobStatus.Completed);

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

        private async Task RunJob(Job job, CancellationToken token)
        {
            try
            {
                _logger.LogInformation($"Trying to acquire lock for job {job.Id}.");

                using var jobLock = await _jobRepository.AcquireJobLock(job.Id, token);

                if (jobLock.Status != JobStatus.Active)
                {
                    jobLock.Abandon();
                    return;
                }

                if (jobLock.Due > DateTime.UtcNow)
                {
                    _logger.LogInformation($"Acquired lock but job {job.Id} not due yet. Next due: {jobLock.Due}.");

                    jobLock.Abandon();

                    return;
                }

                _logger.LogInformation($"Acquired lock, beginning job {job.Id}.");

                try
                {
                    var selector = JsonSerializer.Deserialize<HtmlElementSelection>(job.Selector, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation($"Running selector {job.Xpath} for {job.Url} for job {job.Id}.");

                    var result = await _crawler.GetPrice(job.Url, selector, job.Xpath, token);

                    if (result.IsSuccess && result.Price.HasValue)
                    {
                        _logger.LogInformation($"Job completed successfully and found price {result.Price.Value} for job {job.Id}.");

                        await jobLock.Complete(result.Price.Value, result.Log);
                    }
                    else
                    {
                        _logger.LogWarning($"Job failed. Job was {job.Id}.");

                        await jobLock.CompleteWithError(result.Log);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, $"Failed job run for {job.Url} ({job.Id}) due to an error.");
                    jobLock.Abandon();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"Failed to acquire job lock for job {job.Id}.");
                // ignored
            }
        }
    }
}
