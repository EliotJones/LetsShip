using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App.Jobs
{
    public class JobData
    {
        public IReadOnlyList<DataPoint> DataPoints { get; set; } = Array.Empty<DataPoint>();

        public Uri Website { get; set; } = new Uri("about:empty");

        public class DataPoint
        {
            public decimal Value { get; set; }

            public DateTime Date { get; set; }
        }
    }

    public class GetJobData : IRequest<JobData>
    {
        public string Token { get; }

        public GetJobData(string token)
        {
            Token = token;
        }
    }

    internal class GetJobDataHandler : IRequestHandler<GetJobData, JobData>
    {
        private readonly IJobRepository _jobRepository;

        public GetJobDataHandler(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<JobData> Handle(GetJobData request, CancellationToken cancellationToken)
        {
            var job = await _jobRepository.GetByToken(request.Token);
            var runs = await _jobRepository.GetJobRunsByJobId(job.Id);

            return new JobData
            {
                Website = job.Url,
                DataPoints = runs.Select(x => new JobData.DataPoint
                {
                    Value = x.Price.GetValueOrDefault(),
                    Date = x.Created
                }).ToList()
            };
        }
    }
}
