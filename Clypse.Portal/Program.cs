using Blazing.Mvvm;
using Devoctomy.Blazwiz.Auth.Cognito.Extensions;
using Devoctomy.Blazwiz.Core.Extensions;
using Devoctomy.Blazwiz.UI.Extensions;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Clypse.Core.Extensions;
using Clypse.Portal;
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

// Sync and end to end encryption, and the Sync page: the settings from sync.json (embedded in this project), and the
// profiles (what is synced, and where it is kept on the device) from the SyncProfileDefinition classes in the app.
builder.Services.AddBlazwizSyncModule(builder.Configuration, sync => sync
    .UseEmbeddedConfiguration(typeof(Program).Assembly, "sync.json")
    .AddProfilesFromAssemblyContaining<Program>()
    .AddProfilesFromAssemblyContaining<DashboardViewModel>());

// The clypse domain: password generation and complexity estimation, the embedded text lists, and secret import.
// The full weak known passwords list (over five million entries) is far too slow to load in a Debug WebAssembly build,
// so Debug builds check passwords against a small stand-in list instead.
builder.Services.AddClypseCore(textLists =>
{
#if DEBUG
    textLists.UseFullWeakKnownPasswordsList = false;
#endif
});

// The app's own services and pages. Replace or turn off framework pages here too, e.g.
// builder.Services.ReplacePage<MyLogin>(BlazwizPages.Login) or builder.Services.DisablePage(BlazwizPages.Sync).
builder.Services.AddAppServices();

// The app's view models: all of them in the application assembly are registered automatically.
builder.Services.AddMvvm(options =>
{
    options.HostingModelType = BlazorHostingModelType.WebAssembly;
    options.RegisterViewModelsFromAssemblyContaining<DashboardViewModel>();
});

var host = builder.Build();

// The language the user chose in Settings (or the browser's), applied before the app starts. Add languages with
// Text:SupportedCultures in wwwroot/appsettings.json, and their text with resources (see the README).
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ICultureService>().ApplySavedCultureAsync();
}

await host.RunAsync();
