using clypse.core.setup.Extensions;
using clypse.portal.setup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace clypse.portal.setup;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();

        var program = host.Services.GetService<IProgram>();
        return await program!.Run();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
            services
                .AddClypseSetupServices());
}
