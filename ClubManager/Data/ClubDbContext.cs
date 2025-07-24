using Microsoft.EntityFrameworkCore;
using ClubManager.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClubManager.Data
{
    public class ClubDbContext : DbContext
    {
        public DbSet<Abonado> Abonados { get; set; } = null!;
        public DbSet<Gestor> Gestores { get; set; } = null!;
        public DbSet<Peña> Peñas { get; set; } = null!;
        public DbSet<TipoAbono> TiposAbono { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<UserPermissions> UserPermissions { get; set; } = null!;
        public DbSet<HistorialAccion> HistorialAcciones { get; set; } = null!;
        public DbSet<Configuracion> Configuracion { get; set; } = null!; // ✅ AÑADIDO

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

            // Configuraciones de entidades
            ConfigureAbonado(modelBuilder);
            ConfigureUsuario(modelBuilder);
            ConfigureConfiguracion(modelBuilder); // ✅ AÑADIDO
            SeedData(modelBuilder);
        }

        private void ConfigureAbonado(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Abonado>(entity =>
            {
                entity.HasIndex(e => e.NumeroSocio).IsUnique();
                entity.HasIndex(e => e.DNI).IsUnique();
                entity.HasIndex(e => e.CodigoBarras).IsUnique();

                entity.HasOne(e => e.Gestor).WithMany(g => g.Abonados).HasForeignKey(e => e.GestorId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Peña).WithMany(p => p.Abonados).HasForeignKey(e => e.PeñaId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.TipoAbono).WithMany(t => t.Abonados).HasForeignKey(e => e.TipoAbonoId).OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(e => e.Permissions).WithOne(p => p.Usuario).HasForeignKey<UserPermissions>(p => p.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HistorialAccion>(entity =>
            {
                entity.HasOne(e => e.Usuario).WithMany(u => u.HistorialAcciones).HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ✅ MÉTODO AÑADIDO
        private void ConfigureConfiguracion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuracion>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configurar propiedades
                entity.Property(e => e.NombreClub).HasMaxLength(200);
                entity.Property(e => e.TemporadaActual).HasMaxLength(50);
                entity.Property(e => e.DireccionClub).HasMaxLength(500);
                entity.Property(e => e.TelefonoClub).HasMaxLength(50);
                entity.Property(e => e.EmailClub).HasMaxLength(100);
                entity.Property(e => e.WebClub).HasMaxLength(200);
                entity.Property(e => e.LogoClub).HasMaxLength(500);
                entity.Property(e => e.RutaEscudo).HasMaxLength(500);
                entity.Property(e => e.FormatoNumeroSocio).HasMaxLength(50);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Usuario administrador por defecto
            modelBuilder.Entity<Usuario>().HasData(new Usuario
            {
                Id = 1,
                NombreUsuario = "admin",
                PasswordHash = HashPassword("admin123"),
                NombreCompleto = "Administrador",
                Email = "admin@clubmanager.com",
                Rol = "Administrador",
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            // Permisos completos para admin
            modelBuilder.Entity<UserPermissions>().HasData(new UserPermissions
            {
                Id = 1,
                UsuarioId = 1,
                CanAccessDashboard = true,
                CanAccessAbonados = true,
                CanAccessTiposAbono = true,
                CanAccessGestores = true,
                CanAccessPeñas = true,
                CanAccessUsuarios = true,
                CanAccessHistorial = true,
                CanAccessConfiguracion = true,
                CanAccessTemplates = true,
                CanEdit = true,
                CanDelete = true,
                CanExport = true,
                CanPrint = true,
                CanCreateAbonados = true,
                CanEditAbonados = true,
                CanDeleteAbonados = true,
                CanExportData = true,
                CanImportData = true,
                CanManageSeasons = true,
                CanChangeLicense = true,
                CanCreateBackups = true
            });

            // ✅ CONFIGURACIÓN INICIAL
            modelBuilder.Entity<Configuracion>().HasData(new Configuracion
            {
                Id = 1,
                NombreClub = "Mi Club Deportivo",
                TemporadaActual = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                DireccionClub = "",
                TelefonoClub = "",
                EmailClub = "",
                WebClub = "",
                LogoClub = "",
                RutaEscudo = "",
                AutoBackup = true,
                ConfirmarEliminaciones = true,
                MostrarAyudas = true,
                NumeracionAutomatica = true,
                FormatoNumeroSocio = "simple",
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            });

            // Datos iniciales
            modelBuilder.Entity<Gestor>().HasData(
                new Gestor { Id = 1, Nombre = "Gestor Principal", FechaCreacion = DateTime.Now },
                new Gestor { Id = 2, Nombre = "Gestor Secundario", FechaCreacion = DateTime.Now }
            );

            modelBuilder.Entity<Peña>().HasData(
                new Peña { Id = 1, Nombre = "Peña Norte" },
                new Peña { Id = 2, Nombre = "Peña Sur" },
                new Peña { Id = 3, Nombre = "Peña Este" },
                new Peña { Id = 4, Nombre = "Sin Peña" }
            );

            modelBuilder.Entity<TipoAbono>().HasData(
                new TipoAbono { Id = 1, Nombre = "Abono General", Precio = 150.00m, Activo = true, FechaCreacion = DateTime.Now },
                new TipoAbono { Id = 2, Nombre = "Abono Jubilado", Precio = 75.00m, Activo = true, FechaCreacion = DateTime.Now },
                new TipoAbono { Id = 3, Nombre = "Abono Infantil", Precio = 50.00m, Activo = true, FechaCreacion = DateTime.Now }
            );
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}