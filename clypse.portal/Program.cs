using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Extensions;
using clypse.portal;
using clypse.portal.Extensions;
using clypse.portal.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IVaultManagerFactoryService, VaultManagerFactoryService>();
builder.Services.AddScoped<IVaultManagerBootstrapperFactoryService, VaultManagerBootstrapperFactoryService>();
builder.Services.AddScoped<IVaultStorageService, VaultStorageService>();
builder.Services.AddScoped<IAuthenticationService, AwsCognitoAuthenticationService>();
builder.Services.AddScoped<IPwaUpdateService, PwaUpdateService>();
builder.Services.AddScoped<ICryptoService, BouncyCastleAesGcmCryptoService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

// Add Clypse core services
builder.Services.AddClypseCoreServices();

var settings = builder.GetSettings();

// Default key derivation service options
builder.Services.AddSingleton(
    settings.AppSettings.TestMode ?
    KeyDerivationServiceDefaultOptions.Blazor_Argon2id_Test() :
    KeyDerivationServiceDefaultOptions.Blazor_Argon2id());

var app = builder.Build();
await app.RunAsync();
