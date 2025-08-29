using clypse.portal;
using clypse.portal.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configure AWS Cognito settings from appsettings.json
var cognitoConfig = new AwsCognitoConfig();
builder.Configuration.GetSection("AwsCognito").Bind(cognitoConfig);
builder.Services.AddSingleton(cognitoConfig);

await builder.Build().RunAsync();
