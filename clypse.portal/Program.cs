using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Password;
using clypse.portal;
using clypse.portal.Models;
using clypse.portal.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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
builder.Services.AddScoped<IRandomGeneratorService, RandomGeneratorService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
builder.Services.AddScoped<IKeyDerivationService, KeyDerivationService>();

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

await builder.Build().RunAsync();
