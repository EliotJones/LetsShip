using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriceFalcon.App.Registration;
using PriceFalcon.Infrastructure;
using PriceFalcon.Web.Services;

namespace PriceFalcon.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.Get<PriceFalconConfig>();
            config.SendGridApiKey = Configuration.GetValue<string>("PRICE_FALCON_SENDGRID");

            services.AddSingleton(config);

            Bootstrapper.Start(services, Configuration);

            services.AddControllersWithViews();
            services.AddMediatR(typeof(SendEmailInvite).Assembly);

            services.AddSingleton<RequestLogQueue>();
            services.AddHostedService<RequestLogQueueConsumer>();
            services.AddHostedService<EmailPriceChangeNotifyService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<RequestLogMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            Bootstrapper.MigrateDatabase(Configuration);
        }
    }
}
