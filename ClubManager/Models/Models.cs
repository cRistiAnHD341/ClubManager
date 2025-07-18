using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManager.Models
{
    public class Abonado
    {
        public int Id { get; set; }

        [Required]
        public int NumeroSocio { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string DNI { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        public EstadoAbonado Estado { get; set; } = EstadoAbonado.Inactivo;

        public bool Impreso { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public int? GestorId { get; set; }
        public virtual Gestor? Gestor { get; set; }

        public int? PeñaId { get; set; }
        public virtual Peña? Peña { get; set; }

        public int? TipoAbonoId { get; set; }
        public virtual TipoAbono? TipoAbono { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public string NombreCompleto => $"{Nombre} {Apellidos}";

        [NotMapped]
        public string EstadoTexto => Estado == EstadoAbonado.Activo ? "Activo" : "Inactivo";

        [NotMapped]
        public string ColorEstado => Estado == EstadoAbonado.Activo ? "#4CAF50" : "#F44336"; // Verde o Rojo

        [NotMapped]
        public string ColorImpresion => Impreso ? "#FF9800" : "Transparent"; // Amarillo si impreso
    }

    public enum EstadoAbonado
    {
        Inactivo = 0,
        Activo = 1
    }

    public class Gestor
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }

    public class Peña
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Relaciones
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }

    public class TipoAbono
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        // Relaciones
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }

    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Rol { get; set; } = "Usuario";

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public virtual ICollection<HistorialAccion> HistorialAcciones { get; set; } = new List<HistorialAccion>();
    }

    public class HistorialAccion
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Accion { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Detalle { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;
    }

    public class Configuracion
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombreClub { get; set; } = "Mi Club Deportivo";

        [MaxLength(500)]
        public string? RutaEscudo { get; set; }

        [MaxLength(50)]
        public string? TemporadaActual { get; set; }

        public DateTime? FechaUltimaTemporada { get; set; }

        public DateTime FechaModificacion { get; set; } = DateTime.Now;
    }

    // Clases para manejo de licencias
    public class LicenseData
    {
        public string ClubName { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public DateTime GenerationDate { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public class SignedLicense
    {
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class LicenseInfo
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsReadOnly => IsExpired || !IsValid;
    }
}