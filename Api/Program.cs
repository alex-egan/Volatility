using Api.Models.Database;
using Api.Services;
using Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlite(builder.Configuration["ConnectionString"]),
    ServiceLifetime.Scoped);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddHostedService<BeverageWorker>();

builder.Services.AddScoped<BeverageService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

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