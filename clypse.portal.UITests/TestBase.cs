using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace clypse.portal.UITests;

[TestClass]
public class TestBase : PageTest
{
    private static Process? _serverProcess;
    protected static readonly string ServerUrl = "https://localhost:7153";
    private static DeviceProfile? _currentProfile;
    private static string _screenshotDirectory = string.Empty;
    private int _screenshotCounter = 0;

    static TestBase()
    {
        LoadDeviceProfile();
    }

    private static void LoadDeviceProfile()
    {
        // Get profile name from environment variable or use default
        var profileName = Environment.GetEnvironmentVariable("CLYPSE_UITEST_PROFILE") ?? "s24ultra";
        
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testProjectDir = Path.GetDirectoryName(assemblyLocation);
        var profilePath = Path.Combine(testProjectDir!, "Profiles", $"{profileName}.json");

        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException($"Device profile not found: {profilePath}. Set CLYPSE_UITEST_PROFILE environment variable to specify a profile.");
        }

        var json = File.ReadAllText(profilePath);
        _currentProfile = JsonSerializer.Deserialize<DeviceProfile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (_currentProfile == null)
        {
            throw new InvalidOperationException($"Failed to load device profile from: {profilePath}");
        }

        // Set up screenshot directory based on profile
        _screenshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "Screenshots", _currentProfile.Name);
        
        Console.WriteLine($"Loaded device profile: {_currentProfile.Name}");
        Console.WriteLine($"  Physical Resolution: {_currentProfile.ViewportWidth}x{_currentProfile.ViewportHeight}");
        Console.WriteLine($"  Status Bar Height: {_currentProfile.StatusBarHeight}px");
        Console.WriteLine($"  Device Scale Factor: {_currentProfile.DeviceScaleFactor}");
        var effectiveHeight = _currentProfile.ViewportHeight - _currentProfile.StatusBarHeight;
        Console.WriteLine($"  CSS Viewport: {_currentProfile.ViewportWidth / _currentProfile.DeviceScaleFactor}x{effectiveHeight / _currentProfile.DeviceScaleFactor}");
        Console.WriteLine($"  Screenshot Directory: {_screenshotDirectory}");
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        if (_currentProfile == null)
        {
            throw new InvalidOperationException("Device profile not loaded");
        }

        // Calculate CSS viewport from physical resolution and scale factor
        // Subtract status bar height from viewport height before applying scaling
        var effectiveHeight = _currentProfile.ViewportHeight - _currentProfile.StatusBarHeight;
        var cssWidth = (int)(_currentProfile.ViewportWidth / _currentProfile.DeviceScaleFactor);
        var cssHeight = (int)(effectiveHeight / _currentProfile.DeviceScaleFactor);

        return new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize
            {
                Width = cssWidth,
                Height = cssHeight
            },
            DeviceScaleFactor = _currentProfile.DeviceScaleFactor
        };
    }

    [TestInitialize]
    public void BaseTestInitialize()
    {
        // Reset screenshot counter for each test
        _screenshotCounter = 0;
        
        // Ensure screenshot directory exists
        Directory.CreateDirectory(_screenshotDirectory);
    }

    /// <summary>
    /// Takes a screenshot after navigation/page load
    /// </summary>
    protected async Task ScreenshotAfterNavigationAsync(string pageName)
    {
        _screenshotCounter++;
        var testName = TestContext.TestName ?? "UnknownTest";
        var fileName = $"{testName}_{_screenshotCounter:D2}_Navigation_{pageName}.png";
        var filePath = Path.Combine(_screenshotDirectory, fileName);
        
        await Page.ScreenshotAsync(new() { Path = filePath, FullPage = true });
        Console.WriteLine($"Screenshot saved: {fileName}");
    }

    /// <summary>
    /// Takes a screenshot after an input/control action
    /// </summary>
    protected async Task ScreenshotAfterActionAsync(string actionDescription)
    {
        _screenshotCounter++;
        var testName = TestContext.TestName ?? "UnknownTest";
        var fileName = $"{testName}_{_screenshotCounter:D2}_Action_{SanitizeFileName(actionDescription)}.png";
        var filePath = Path.Combine(_screenshotDirectory, fileName);
        
        await Page.ScreenshotAsync(new() { Path = filePath, FullPage = true });
        Console.WriteLine($"Screenshot saved: {fileName}");
    }

    /// <summary>
    /// Takes a screenshot at the end of a test scenario
    /// </summary>
    protected async Task ScreenshotEndOfScenarioAsync()
    {
        _screenshotCounter++;
        var testName = TestContext.TestName ?? "UnknownTest";
        var fileName = $"{testName}_{_screenshotCounter:D2}_EndOfScenario.png";
        var filePath = Path.Combine(_screenshotDirectory, fileName);
        
        await Page.ScreenshotAsync(new() { Path = filePath, FullPage = true });
        Console.WriteLine($"Screenshot saved: {fileName}");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
    }

    private static string GetProjectPath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testProjectDir = Path.GetDirectoryName(assemblyLocation);
        var solutionRoot = (Directory.GetParent(testProjectDir!)?.Parent?.Parent?.Parent?.FullName) ?? throw new DirectoryNotFoundException("Could not locate solution root directory");
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

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --configuration Release --urls=\"{ServerUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        Console.WriteLine($"Starting server with command: dotnet {_serverProcess.StartInfo.Arguments}");

        _serverProcess.Start();

        Console.WriteLine("Server process started, waiting for it to be ready...");

        await WaitForServerAsync();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
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
        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Playwright-Test");

        var retries = 60;
        var delay = 1000;

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