using System;
using System.Linq;
using Claudia.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz.Impl;
using Quartz.Spi;

namespace Claudia.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void UseQuartz(this IServiceCollection services, params Type[] jobs)
        {
            services.AddTransient<IJobFactory, QuartzJobFactory>();
            services.Add(jobs.Select(jobType => new ServiceDescriptor(jobType, jobType, ServiceLifetime.Singleton)));

            services.AddSingleton(async provider =>
            {
                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = await schedulerFactory.GetScheduler();
                scheduler.JobFactory = provider.GetRequiredService<IJobFactory>();
                
                await scheduler.Start();
                return scheduler;
            });
        }
   
    }
}