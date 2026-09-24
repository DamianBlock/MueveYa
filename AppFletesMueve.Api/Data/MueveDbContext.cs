using AppFletesMueve.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Data
{
    public class MueveDbContext : DbContext
    {
        public MueveDbContext(DbContextOptions<MueveDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
    }
}