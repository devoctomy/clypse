using Blazing.Mvvm;
using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Extensions;
using clypse.portal;
using clypse.portal.Application.Extensions;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Services;
using CommunityToolkit.Mvvm.Messaging;
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

// Register browser abstraction implementations
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<IBrowserInteropService, BrowserInteropService>();
builder.Services.AddScoped<IWebAuthnService, WebAuthnService>();
builder.Services.AddScoped<IJsS3InvokerProvider, JsS3InvokerProvider>();

// Register messenger for cross-ViewModel communication
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

var settings = builder.GetSettings();

// Default key derivation service options
builder.Services.AddSingleton(
    settings.AppSettings.TestMode ?
    KeyDerivationServiceDefaultOptions.Blazor_Argon2id_Test() :
    KeyDerivationServiceDefaultOptions.Blazor_Argon2id());

// Configure MVVM
builder.Services.AddMvvm(options =>
{
    options.HostingModelType = BlazorHostingModelType.WebAssembly;
    options.RegisterViewModelsFromAssemblyContaining<LoginViewModel>();
});

var app = builder.Build();
await app.RunAsync();
