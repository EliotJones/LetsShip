using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Infrastructure
{
    public static class Bootstrapper
    {
        public static void Start(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<IConnectionProvider>(
                ctx => new DefaultConnectionProvider(configuration.GetConnectionString("Default")));

            serviceCollection.AddSingleton<IDataProtectionKeyRepository, DataProtectionKeyRepository>();
            serviceCollection.AddSingleton<IDraftJobRepository, DraftJobRepository>();
            serviceCollection.AddSingleton<IEmailRepository, EmailRepository>();
            serviceCollection.AddSingleton<IJobRepository, JobRepository>();
            serviceCollection.AddSingleton<IRequestLogRepository, RequestLogRepository>();
            serviceCollection.AddSingleton<ITokenRepository, TokenRepository>();
            serviceCollection.AddSingleton<IUserRepository, UserRepository>();

            serviceCollection.AddSingleton<IEmailService, SendGridEmailService>();
            serviceCollection.AddSingleton<ITokenService, TokenService>();
        }

        public static void MigrateDatabase(IConfiguration configuration)
        {
            try
            {
                var cnx = new NpgsqlConnection(configuration.GetConnectionString("Default"));
                var evolve = new Evolve.Evolve(cnx, msg => Trace.WriteLine(msg))
                {
                    EmbeddedResourceAssemblies = new []{ typeof(Bootstrapper).Assembly },
                    IsEraseDisabled = true,
                };

                evolve.Migrate();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Database migration failed: {ex}");
                throw;
            }
        }
    }
}
