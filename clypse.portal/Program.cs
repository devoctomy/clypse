using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Extensions;
using clypse.core.Password;
using clypse.portal;
using clypse.portal.Models;
using clypse.portal.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using clypse.core.Data;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Default key derivation service options
builder.Services.AddSingleton(KeyDerivationServiceDefaultOptions.Blazor_Argon2id());

// Register services
builder.Services.AddScoped<IVaultManagerFactoryService, VaultManagerFactoryService>();
builder.Services.AddScoped<IVaultManagerBootstrapperFactoryService, VaultManagerBootstrapperFactoryService>();
builder.Services.AddScoped<IVaultStorageService, VaultStorageService>();
builder.Services.AddScoped<IAuthenticationService, AwsCognitoAuthenticationService>();

// Add Clypse core services
builder.Services.AddClypseCoreServices();

// Configure AWS Cognito settings from appsettings.json
var cognitoConfig = new AwsCognitoConfig();
builder.Configuration.GetSection("AwsCognito").Bind(cognitoConfig);
builder.Services.AddSingleton(cognitoConfig);

// Configure AWS S3 settings from appsettings.json
var awsS3Config = new AwsS3Config();
builder.Configuration.GetSection("AwsS3").Bind(awsS3Config);
builder.Services.AddSingleton(awsS3Config);

// Configure App settings from appsettings.json
var appSettings = new AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);
builder.Services.AddSingleton(appSettings);

var app = builder.Build();

var httpClient = app.Services.GetRequiredService<HttpClient>();
var dataPrefetchService = app.Services.GetRequiredService<IDataPrefetchService>();
try
{
    Console.WriteLine("Loading weak passwords data during startup...");
    var weakPasswordsData = await httpClient.GetStringAsync("/data/dictionaries/weakknownpasswords.txt");
    
    if (!string.IsNullOrEmpty(weakPasswordsData))
    {
        var lines = weakPasswordsData
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        
        dataPrefetchService.PrefetchLines("weakknownpasswords", lines);
        Console.WriteLine($"Loaded {lines.Count} weak passwords during startup");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading weak passwords during startup: {ex.Message}");
}

await app.RunAsync();
