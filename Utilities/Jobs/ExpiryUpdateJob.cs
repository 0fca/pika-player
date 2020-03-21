using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Npgsql;

namespace Claudia.Utilities.Jobs
{
    /**
     * <summary>This class represents job responsible for updating expired videos list</summary>
     * <seealso cref="IJob"/>
     */
    public class ExpiryUpdateJob : IJob
    {
        //This is logger injected by DI.
        private readonly ILogger<ExpiryUpdateJob> _log;
        //This is configuration object injected by DI.
        private readonly IConfiguration _configuration;
        // other dependencies will probably go here
        private readonly IServiceProvider _serviceProvider;

        public ExpiryUpdateJob(ILogger<ExpiryUpdateJob> log, 
                               IConfiguration configuration,
                               IServiceProvider serviceProvider)
        {
            _log = log;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }
        /**
         * <summary>Execute method used by executor object of Quartz.NET</summary>
         */
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var val = context.MergedJobDataMap.Get("VideoStorageTime");
                var connectionString = context.MergedJobDataMap.GetString("ConnectionString");
                var days = int.Parse(val.ToString());
                await Execute(days, connectionString);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

           
        }

        //Private implementation of real logic of this job used in above Execute method.
        private static async Task Execute(int videoStorageTime, string connectionString)
        {
            using(var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                var cmd = new NpgsqlCommand($"UPDATE \"Lectures\" SET is_locked = TRUE WHERE date_added <= date(now() - INTERVAL '{videoStorageTime} days')", conn);
                var status = await cmd.ExecuteNonQueryAsync();

                if (status >= 0)
                {
                    Console.WriteLine("Query ran successfully.");
                }
                conn.Close();
            }
        }
    }
}