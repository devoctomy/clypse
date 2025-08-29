using clypse.portal;
using clypse.portal.Models;
using clypse.portal.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configure AWS Cognito settings
var cognitoConfig = new AwsCognitoConfig();
builder.Configuration.GetSection("AwsCognito").Bind(cognitoConfig);
builder.Services.AddSingleton(cognitoConfig);

// Register Cognito Auth Service
builder.Services.AddScoped<ICognitoAuthService, CognitoAuthService>();

await builder.Build().RunAsync();
