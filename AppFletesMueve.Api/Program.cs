using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC / API
builder.Services.AddControllers();

// Entity Framework Core + SQL Server LocalDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=mueve.db";

builder.Services.AddDbContext<MueveDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Lo dejamos comentado porque estamos usando Cloudflare
// sobre http://localhost:5051
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();