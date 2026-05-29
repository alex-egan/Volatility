using Api.Hubs;
using Api.Models.Database;
using Api.Services;
using Api.Workers;
using Api.Engines;
using Microsoft.EntityFrameworkCore;
using Api.Models.DTO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlite(builder.Configuration["ConnectionString"]),
    ServiceLifetime.Scoped);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSingleton<IMarketConfigService, MarketConfigService>();
builder.Services.AddSingleton<MarketSimulator>();
builder.Services.AddSingleton<PricingEngine>();
builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<BarState>();
builder.Services.AddSingleton<Dictionary<Guid, DrinkState>>();

builder.Services.AddHostedService<DrinkWorker>();
builder.Services.AddHostedService<ConfigRefreshService>();

builder.Services.AddScoped<DrinkService>();

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DBContext>();
    var simulator = scope.ServiceProvider.GetRequiredService<MarketSimulator>();

    var drinks = await db.Drinks
        .Where(b => b.Active)
        .ToListAsync();

    foreach (Drink drink in drinks)
    {
        simulator.AddDrink(new(drink));
    }
}

app.MapHub<DrinkHub>("/drinkhub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(options => {
        options.EnableTryItOut = true;
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();