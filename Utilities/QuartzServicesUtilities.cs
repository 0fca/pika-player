using System;
using Quartz;

namespace Claudia.Utilities
{
    /**
     * <summary>Utility class for configuring and starting Quartz.NET engine to execute jobs.</summary>
     * 
     */
    public static class QuartzServicesUtilities
    {
        public static void StartJob<TJob>(IScheduler scheduler, 
            int runInterval, 
            int videoStorageDayCount,
            string connectionString)
            where TJob : IJob
        {
            var jobName = typeof(TJob).FullName;
            var jobData = new JobDataMap();
            jobData.Put("VideoStorageTime", videoStorageDayCount);
            jobData.Put("ConnectionString", connectionString);

            var job = JobBuilder.Create<TJob>()
                .WithIdentity(jobName)
                .SetJobData(jobData)
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{jobName}.trigger")
                .StartAt(DateTimeOffset.Now.AddSeconds(runInterval))
                .WithSimpleSchedule(scheduleBuilder =>
                    scheduleBuilder
                        .WithIntervalInSeconds(runInterval)
                        .RepeatForever())
                .Build();
            
            scheduler.ScheduleJob(job, trigger).Wait();
        }
    }
}