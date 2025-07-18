using Microsoft.EntityFrameworkCore;
using ClubManager.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClubManager.Data
{
    public class ClubDbContext : DbContext
    {
        public DbSet<Abonado> Abonados { get; set; }
        public DbSet<Gestor> Gestores { get; set; }
        public DbSet<Peña> Peñas { get; set; }
        public DbSet<TipoAbono> TiposAbono { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<HistorialAccion> HistorialAcciones { get; set; }
        public DbSet<Configuracion> Configuracion { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClubManager.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Abonado
            modelBuilder.Entity<Abonado>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NumeroSocio).IsUnique();
                entity.HasIndex(e => e.DNI).IsUnique();
                entity.HasIndex(e => e.CodigoBarras).IsUnique();

                entity.HasOne(e => e.Gestor)
                    .WithMany(g => g.Abonados)
                    .HasForeignKey(e => e.GestorId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Peña)
                    .WithMany(p => p.Abonados)
                    .HasForeignKey(e => e.PeñaId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.TipoAbono)
                    .WithMany(t => t.Abonados)
                    .HasForeignKey(e => e.TipoAbonoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de Gestor
            modelBuilder.Entity<Gestor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Nombre).IsUnique();
            });

            // Configuración de Peña
            modelBuilder.Entity<Peña>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Nombre).IsUnique();
            });

            // Configuración de TipoAbono
            modelBuilder.Entity<TipoAbono>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Nombre).IsUnique();
                entity.Property(e => e.Precio).HasPrecision(10, 2);
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
            });

            // Configuración de HistorialAccion
            modelBuilder.Entity<HistorialAccion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.HistorialAcciones)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Configuracion
            modelBuilder.Entity<Configuracion>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // Datos iniciales
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Configuración inicial
            modelBuilder.Entity<Configuracion>().HasData(
                new Configuracion
                {
                    Id = 1,
                    NombreClub = "Mi Club Deportivo",
                    TemporadaActual = "2024-2025",
                    FechaUltimaTemporada = DateTime.Now,
                    FechaModificacion = DateTime.Now
                }
            );

            // Usuario administrador por defecto
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    NombreUsuario = "admin",
                    PasswordHash = HashPassword("admin123"), // Contraseña por defecto
                    Rol = "Administrador",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                }
            );

            // Gestores iniciales
            modelBuilder.Entity<Gestor>().HasData(
                new Gestor { Id = 1, Nombre = "Gestor Principal", FechaCreacion = DateTime.Now },
                new Gestor { Id = 2, Nombre = "Gestor Secundario", FechaCreacion = DateTime.Now }
            );

            // Peñas iniciales
            modelBuilder.Entity<Peña>().HasData(
                new Peña { Id = 1, Nombre = "Peña Norte" },
                new Peña { Id = 2, Nombre = "Peña Sur" },
                new Peña { Id = 3, Nombre = "Peña Este" },
                new Peña { Id = 4, Nombre = "Sin Peña" }
            );

            // Tipos de abono iniciales
            modelBuilder.Entity<TipoAbono>().HasData(
                new TipoAbono { Id = 1, Nombre = "Abono General", Precio = 150.00m },
                new TipoAbono { Id = 2, Nombre = "Abono Jubilado", Precio = 75.00m },
                new TipoAbono { Id = 3, Nombre = "Abono Infantil", Precio = 50.00m },
                new TipoAbono { Id = 4, Nombre = "Abono Familiar", Precio = 400.00m }
            );
        }

        private string HashPassword(string password)
        {
            // Simple hash para el ejemplo - en producción usar algo más seguro como BCrypt
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                await Database.EnsureCreatedAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al inicializar la base de datos: {ex.Message}");
                return false;
            }
        }
    }
}