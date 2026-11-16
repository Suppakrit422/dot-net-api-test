using dot_net_api.Data;
using dot_net_api.Repositories;
using dot_net_api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<QueueDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository + service layers (one instance per HTTP request, same lifetime as the DbContext).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IQueueService, QueueService>();

// CORS: let the web app (a different origin) call this API from the browser.
// Allowed origins come from config ("Cors:AllowedOrigins"), so each environment lists its own front-end URLs.
const string WebAppCorsPolicy = "WebApp";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(WebAppCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Location")));   // lets the web app read the new ticket's URL from POST /api/queue

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "dot-net-api v1"));
}

app.UseHttpsRedirection();

app.UseCors(WebAppCorsPolicy);   // must run before UseAuthorization

app.UseAuthorization();

app.MapControllers();

app.Run();
