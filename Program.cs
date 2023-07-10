using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BNHPortalServices
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                //Register our services with the DI framework
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPortalPublicKeyProvider, PortalPublicKeyProvider>();
                    services.AddScoped<IUserInfoProvider, UserInfoProvider>();
                })

                //Register our middleware
                .ConfigureFunctionsWorkerDefaults(workerApplicationBuilder =>
                {
                    workerApplicationBuilder.UseMiddleware<AuthorizationMiddleware>();
                })
                .Build();

            host.Run();
        }
    }
}