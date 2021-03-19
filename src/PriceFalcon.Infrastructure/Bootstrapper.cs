using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.Infrastructure
{
    public static class Bootstrapper
    {
        public static void Start(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<IConnectionProvider>(
                ctx => new DefaultConnectionProvider(configuration.GetConnectionString("Default")));
            serviceCollection.AddSingleton<IUserRepository, UserRepository>();
            serviceCollection.AddSingleton<ITokenService, TokenService>();
            serviceCollection.AddSingleton<IEmailService, SendGridEmailService>();
        }
    }
}
