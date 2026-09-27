using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=mueve.db";

builder.Services.AddDbContext<MueveDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migraciones pendientes automáticamente al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MueveDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();