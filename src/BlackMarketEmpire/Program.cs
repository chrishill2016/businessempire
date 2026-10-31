using BlackMarketEmpire;
using BlackMarketEmpire.Models;
using BlackMarketEmpire.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 2;
    config.SnackbarConfiguration.VisibleStateDuration = 2500;
    config.SnackbarConfiguration.ShowTransitionDuration = 150;
    config.SnackbarConfiguration.HideTransitionDuration = 150;
});

builder.Services.AddSingleton(GameRules.Default);
builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
builder.Services.AddSingleton<MarketService>();
builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<EncounterService>();
builder.Services.AddSingleton<ShippingService>();
builder.Services.AddSingleton<FinanceService>();
builder.Services.AddSingleton<IntelService>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<ILocalStore, IndexedDbStore>();
builder.Services.AddSingleton<HighScoreService>();
builder.Services.AddSingleton<GameSession>();
builder.Services.AddSingleton<AppStatusService>();

await builder.Build().RunAsync();
