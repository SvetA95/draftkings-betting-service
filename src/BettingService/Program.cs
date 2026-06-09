using BettingService.Data;
using BettingService.Endpoints;
using BettingService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");
if (usePostgres)
{
    builder.Services.AddDbContext<BettingDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
}
else
{
    builder.Services.AddDbContext<BettingDbContext>(options =>
        options.UseSqlite("Data Source=betting.db"));
}

builder.Services.AddSingleton<AsyncProcessingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AsyncProcessingService>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Betting Service", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

app.MapUserEndpoints();
app.MapBetEndpoints();
app.MapMarketEndpoints();
app.MapEventEndpoints();
app.MapCashoutEndpoints();
app.MapAdminEndpoints();

app.MapFallback(async context =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(
            Path.Combine(app.Environment.WebRootPath, "index.html"));
    }
});

app.Run();
