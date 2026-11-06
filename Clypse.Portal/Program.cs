using Blazing.Mvvm;
using Devoctomy.Blazwiz.Auth.Cognito.Extensions;
using Devoctomy.Blazwiz.Core.Extensions;
using Devoctomy.Blazwiz.UI.Extensions;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Clypse.Core.Extensions;
using Clypse.Portal;
using Clypse.Portal.Components;
using Clypse.Portal.Application;
using Clypse.Portal.Application.Extensions;
using Clypse.Portal.Application.ViewModels;
using Devoctomy.Blazwiz.UI.Sync.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The app's name and ID: everything it keeps in the browser is named after it (e.g. clypse_session).
builder.Services.AddBlazwizApp(AppIdentity.Name, AppIdentity.Id);

// The framework: the app shell (sign in, theme, settings and their pages), with settings from wwwroot/appsettings.json
// (+ appsettings.{Environment}.json), signing in with Cognito once a user pool is configured.
builder.Services.AddBlazwizUi(builder.Configuration);
builder.Services.AddBlazwizCognito(builder.Configuration);

// Sync and end to end encryption, and the Sync page, with the settings from sync.json (embedded in this project).
// Clypse defines no sync profiles in code: every vault is a protected profile the user creates. A vault locks itself
// after 5 minutes without use unless the user chooses otherwise in its settings, and may be unlocked with the passkey
// once the user turns that on for it on a device.
builder.Services.AddBlazwizSyncModule(builder.Configuration, sync => sync
    .UseEmbeddedConfiguration(typeof(Program).Assembly, "sync.json")
    .UseProfileDefaults(vault =>
    {
        vault.IdleTimeout = TimeSpan.FromMinutes(5);
        vault.AllowDeviceUnlock = true;
    }));

// The clypse domain: password generation and complexity estimation, the embedded text lists, and secret import.
// The full weak known passwords list (over five million entries) is far too slow to load in a Debug WebAssembly build,
// so Debug builds check passwords against a small stand-in list instead.
builder.Services.AddClypseCore(textLists =>
{
#if DEBUG
    textLists.UseFullWeakKnownPasswordsList = false;
#endif
});

// Vaults (each a protected sync profile the user creates), their secrets and pages, with the settings from the Clypse
// section of wwwroot/appsettings.json. Replace or turn off framework pages here too, e.g.
// builder.Services.ReplacePage<MyLogin>(BlazwizPages.Login) or builder.Services.DisablePage(BlazwizPages.Sync).
builder.Services.AddClypse(builder.Configuration);
builder.Services.AddSettingsSection<PasswordGeneratorSettingsSection>(order: 300);

// The app's view models: all of them in the application assembly are registered automatically.
builder.Services.AddMvvm(options =>
{
    options.HostingModelType = BlazorHostingModelType.WebAssembly;
    options.RegisterViewModelsFromAssemblyContaining<VaultsViewModel>();
});

var host = builder.Build();

// The language the user chose in Settings (or the browser's), applied before the app starts. Add languages with
// Text:SupportedCultures in wwwroot/appsettings.json, and their text with resources (see the README).
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ICultureService>().ApplySavedCultureAsync();
}

await host.RunAsync();
