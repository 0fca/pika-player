using System;
using System.Threading.Tasks;
using Claudia.Data;
using Claudia.Models;
using Claudia.Services;
using Claudia.Utilities;
using Claudia.Utilities.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Claudia.Extensions;

namespace Claudia
{
    /**
     * <summary>Startup class used for configuring and starting Core, app core, services and others.</summary>
     * 
     */
    public class Startup
    {
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        //Used by Core to configure all services needed by the app. Registers all of them in ServiceProvider and in DI container.
        public void ConfigureServices(IServiceCollection services)
        {
                       
            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<UserContext>()
                .AddDefaultTokenProviders();
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireElevated", policy => policy.RequireRole("Admin","Lecturer"));
                options.AddPolicy("RequireBase", policy => policy.RequireRole("Admin","Lecturer","Student"));
            });
            
            services.AddAuthentication();
            
            var sqlConnectionString = Configuration.GetConnectionString("UserDatabase");
 
            services.AddDbContext<UserContext>(options =>
                options.UseNpgsql(
                    sqlConnectionString
                )
            );
            
            services.AddDbContext<LecturesContext>(options =>
                options.UseNpgsql(
                    sqlConnectionString
                )
            );
            
            services.AddScoped<IGenerator, HashGeneratorService>();
            services.AddScoped<IVideoService, LectureService>();
            services.UseQuartz(typeof(ExpiryUpdateJob));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        //App configuration such as http settings, security settings, cookies, error handling etc.
        public void Configure(IApplicationBuilder app, 
                              IHostingEnvironment env, 
                              IServiceProvider serviceProvider,
                              IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseStatusCodePages();
            applicationLifetime.ApplicationStopping.Register(OnShutdown);            

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            
            var interval = Configuration.GetSection("Quartz")["Interval"];
            var videoStorageTime = Configuration.GetSection("Storage")["VideoStorageTime"];
            var scheduler = serviceProvider.GetRequiredService<Task<IScheduler>>().Result;
            QuartzServicesUtilities.StartJob<ExpiryUpdateJob>(scheduler, 
                int.Parse(interval), int.Parse(videoStorageTime),
                Configuration.GetConnectionString("UserDatabase"));
            
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            CreateRoles(serviceProvider).Wait();
        }

        private void OnShutdown()
        {
            
            
        }

        //Helper method for seeding admin user and roles to the database.
        private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            //adding custom roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            string[] roleNames = { "Admin", "Lecturer", "Student" };

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            
            //creating a super user who could maintain the web app
            var poweruser = new User
            {
                UserName = Configuration.GetSection("UserSettings")["UserEmail"],
                Email = Configuration.GetSection("UserSettings")["UserEmail"]
            };
            
            var userPassword = Configuration.GetSection("UserSettings")["UserPassword"];
            var user = await userManager.FindByEmailAsync(Configuration.GetSection("UserSettings")["UserEmail"]);
            
            //Debug.WriteLine(_user.Email);
            if (user == null)
            {
                var createPowerUser = await userManager.CreateAsync(poweruser, userPassword);
                if (createPowerUser.Succeeded)
                {
                    //here we tie the new user to the "Admin" role 
                    await userManager.AddToRoleAsync(poweruser, "Admin");
                }
            }
        }
    }
}