using clypse.portal;
using clypse.portal.Models;
using clypse.portal.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register VaultManagerFactoryService
builder.Services.AddScoped<IVaultManagerFactoryService, VaultManagerFactoryService>();

// Register VaultStorageService
builder.Services.AddScoped<IVaultStorageService, VaultStorageService>();

// Configure AWS Cognito settings from appsettings.json
var cognitoConfig = new AwsCognitoConfig();
builder.Configuration.GetSection("AwsCognito").Bind(cognitoConfig);
builder.Services.AddSingleton(cognitoConfig);

// Configure App settings from appsettings.json
var appSettings = new AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);
builder.Services.AddSingleton(appSettings);

await builder.Build().RunAsync();
