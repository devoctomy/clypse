using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;

namespace clypse.portal.UITests;

[TestClass]
public class TestBase : PageTest
{
    private static Process? _serverProcess;
    protected static readonly string ServerUrl = "https://localhost:7153";

    private static string GetProjectPath()
    {
        // Get the directory where the test assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testProjectDir = Path.GetDirectoryName(assemblyLocation);

        // Navigate up to the solution root and then to the portal project
        // From: clypse.portal.UITests/bin/Debug/net8.0/
        // To: clypse.portal/
        var solutionRoot = Directory.GetParent(testProjectDir!)?.Parent?.Parent?.Parent?.FullName;
        if (solutionRoot == null)
        {
            throw new DirectoryNotFoundException("Could not locate solution root directory");
        }

        var projectPath = Path.Combine(solutionRoot, "clypse.portal", "clypse.portal.csproj");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Could not find project file at: {projectPath}");
        }

        return projectPath;
    }

    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        Console.WriteLine("Starting test server setup...");

        var projectPath = GetProjectPath();
        Console.WriteLine($"Using project path: {projectPath}");

        // Start the Blazor server using dotnet run
        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --urls=\"{ServerUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                }
            }
        };

        Console.WriteLine($"Starting server with command: dotnet {_serverProcess.StartInfo.Arguments}");

        _serverProcess.Start();

        Console.WriteLine("Server process started, waiting for it to be ready...");

        // Wait for the server to be ready
        await WaitForServerAsync();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup(TestContext context)
    {
        try
        {
            _serverProcess?.Kill(true);
            Console.WriteLine("Server process stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping server: {ex.Message}");
        }
        finally
        {
            _serverProcess?.Dispose();
        }
    }

    private static async Task WaitForServerAsync()
    {
        // Create HttpClient with custom handler to skip SSL validation for local dev
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Playwright-Test");

        var retries = 60; // 60 seconds timeout for Blazor WASM compilation
        var delay = 1000; // 1 second between retries

        while (retries-- > 0)
        {
            try
            {
                Console.WriteLine($"Waiting for server at {ServerUrl}... ({60 - retries}/60)");
                var response = await client.GetAsync(ServerUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Server is ready!");
                    return;
                }
                Console.WriteLine($"Server responded with: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server not ready yet: {ex.GetType().Name} - {ex.Message}");
            }

            await Task.Delay(delay);
        }

        throw new TimeoutException($"Server at {ServerUrl} failed to start within 60 seconds");
    }
}