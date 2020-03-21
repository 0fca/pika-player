using System;
using System.Diagnostics;
using Quartz;
using Quartz.Spi;

namespace Claudia.Services
{
    /**
     * <summary>A job factory used for creating job objects used by Quartz.NET to schedule tasks.</summary>
     * 
     */
    public class QuartzJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QuartzJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobDetail = bundle.JobDetail;

            try
            {
                var job = (IJob) _serviceProvider.GetService(jobDetail.JobType);
                return job;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }

            return null;
        }

        public void ReturnJob(IJob job) { }
    }

}