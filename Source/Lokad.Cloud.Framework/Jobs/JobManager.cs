using System;

namespace Lokad.Cloud.Jobs
{
    public class JobManager
    {
        private readonly ILog _log;

        public JobManager(ILog log)
        {
            _log = log;
        }

        public Job CreateNew()
        {
            return new Job
                {
                    JobId = string.Format("j{0:yyyyMMddHHnnss}{1:N}", DateTime.UtcNow, Guid.NewGuid())
                };
        }

        public Job StartNew()
        {
            var job = CreateNew();
            Start(job);
            return job;
        }

        public void Start(Job job)
        {
            // TODO: Implementation
            _log.DebugFormat("Job {0} started", job.JobId);
        }

        public void Succeed(Job job)
        {
            // TODO: Implementation
            _log.DebugFormat("Job {0} succeeded", job.JobId);
        }

        public void Fail(Job job)
        {
            // TODO: Implementation
            _log.DebugFormat("Job {0} failed", job.JobId);
        }
    }
}
