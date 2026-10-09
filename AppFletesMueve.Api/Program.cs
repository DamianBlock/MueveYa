using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Models;
using AppFletesMueve.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? ConvertirUrlPostgres(databaseUrl)
    : builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<MueveDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddScoped<ICalculadoraTarifas, CalculadoraTarifas>();      

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MueveDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService(typeof(Microsoft.Extensions.Logging.ILogger<Program>)) as Microsoft.Extensions.Logging.ILogger;
        logger?.LogError(ex, "Fallo al aplicar migraciones en arranque");
        // No re-lanzar para evitar que la aplicación se cierre en producción durante diagnóstico
    }

    if (!db.TiposCarga.Any())
    {
        db.TiposCarga.AddRange(
            new TipoCarga { Nombre = "Muebles", PesoEstimadoKg = 40, VolumenEstimadoM3 = 0.8 },
            new TipoCarga { Nombre = "Electrodomésticos", PesoEstimadoKg = 60, VolumenEstimadoM3 = 0.6 },
            new TipoCarga { Nombre = "Cajas y bultos", PesoEstimadoKg = 15, VolumenEstimadoM3 = 0.1 },
            new TipoCarga { Nombre = "Materiales de construcción", PesoEstimadoKg = 200, VolumenEstimadoM3 = 0.5 },
            new TipoCarga { Nombre = "Otros", PesoEstimadoKg = 20, VolumenEstimadoM3 = 0.2 });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<AppFletesMueve.Api.Hubs.SolicitudesHub>("/hubs/solicitudes");
app.Run();

static string ConvertirUrlPostgres(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    return new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        Database = uri.AbsolutePath.TrimStart('/'),
        SslMode = url.Contains("sslmode=require")
            ? Npgsql.SslMode.Require
            : Npgsql.SslMode.Prefer,
        Timeout = 30
    }.ToString();
}