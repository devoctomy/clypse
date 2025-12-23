using clypse.core.Extensions;
using clypse.portal.setup.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddClypseSetupServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Bootstrap");

        var orchestration = serviceProvider.GetRequiredService<IClypseAwsSetupOrchestration>();

        try
        {
            await orchestration.SetupClypseOnAwsAsync(CancellationToken.None);

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the setup process.");
            return -1;
        }
    }
}
