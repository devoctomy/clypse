using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Extensions;
using clypse.portal;
using clypse.portal.Application.Extensions;
using clypse.portal.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<ICryptoService, BouncyCastleAesGcmCryptoService>();

// Add application logic services
builder.Services.AddApplicationLogicServices();

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
