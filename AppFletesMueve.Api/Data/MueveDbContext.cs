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
        public DbSet<Conductor> Conductores { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<TipoCarga> TiposCarga { get; set; }
        public DbSet<SolicitudFlete> SolicitudesFlete { get; set; }
        public DbSet<SolicitudCarga> SolicitudesCarga { get; set; }
        public DbSet<AppFletesMueve.Api.Models.ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Un usuario puede tener como máximo un perfil de conductor
            modelBuilder.Entity<Conductor>(e =>
            {
                e.HasOne(c => c.Usuario)
                    .WithMany()
                    .HasForeignKey(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(c => c.UsuarioId).IsUnique();
            });

            modelBuilder.Entity<Vehiculo>(e =>
            {
                e.HasOne(v => v.Conductor)
                    .WithMany(c => c.Vehiculos)
                    .HasForeignKey(v => v.ConductorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(v => v.Patente).HasMaxLength(10);
                e.HasIndex(v => v.Patente).IsUnique();

                // Se guarda como texto ("Camioneta") para que la tabla sea legible
                e.Property(v => v.TipoVehiculo).HasConversion<string>().HasMaxLength(30);
            });

            modelBuilder.Entity<SolicitudFlete>(e =>
            {
                e.HasOne(s => s.Cliente)
                    .WithMany()
                    .HasForeignKey(s => s.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Conductor)
                    .WithMany()
                    .HasForeignKey(s => s.ConductorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Vehiculo)
                    .WithMany()
                    .HasForeignKey(s => s.VehiculoId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(s => s.TipoServicio).HasConversion<string>().HasMaxLength(20);
                e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(30);
                e.Property(s => s.Precio).HasPrecision(12, 2);

                e.Property(s => s.TipoVehiculo)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .HasDefaultValue(TipoVehiculo.Utilitario);

                e.HasIndex(s => s.Estado);
            });

            modelBuilder.Entity<SolicitudCarga>(e =>
            {
                // Si se borra la solicitud, se borran sus cargas
                e.HasOne(c => c.SolicitudFlete)
                    .WithMany(s => s.Cargas)
                    .HasForeignKey(c => c.SolicitudFleteId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.TipoCarga)
                    .WithMany()
                    .HasForeignKey(c => c.TipoCargaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChatMessage>().HasIndex(m => m.SolicitudFleteId);
        }
    }
}
