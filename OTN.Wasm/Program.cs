using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BlazorBlueprint.Components;
using OTN.Wasm;
using OTN.Wasm.ViewModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<NetworkEditorViewModel>();
builder.Services.AddBlazorBlueprintComponents();

await builder.Build().RunAsync();
