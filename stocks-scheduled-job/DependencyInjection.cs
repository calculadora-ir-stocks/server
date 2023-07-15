using Hangfire;
using stocks_scheduled_job.Hangfire;
using stocks_scheduled_job.Services;

namespace stocks_scheduled_job
{
    public static class DependencyInjection
    {
        public static void ConfigureServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangFireDatabase")));

            // Add the processing server as IHostedService
            services.AddHangfireServer();
        }

        public static async Task InitializeHangFireRecurringJob(this IServiceCollection services, WebApplication? app)
        {
            await app.StartAsync();

            HangfireJobScheduler.ScheduleJob();

            await app.WaitForShutdownAsync();
        }

        public static void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs)
        {
            app.UseStaticFiles();

            app.UseHangfireDashboard();

            backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
