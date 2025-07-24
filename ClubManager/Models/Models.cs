using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ClubManager.Models
{
    // ENTIDADES PRINCIPALES
    public class Abonado
    {
        public int Id { get; set; }

        [Required]
        public int NumeroSocio { get; set; } // Cambiado de int a string

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = "";

        [Required, MaxLength(100)]
        public string Apellidos { get; set; } = "";

        [MaxLength(20)]
        public string? DNI { get; set; } // Cambiado a nullable

        [MaxLength(20)]
        public string? Telefono { get; set; } // Añadido

        [MaxLength(100)]
        public string? Email { get; set; } // Añadido

        [MaxLength(500)]
        public string? Direccion { get; set; } // Añadido

        public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-30); // Añadido

        [MaxLength(50)]
        public string? CodigoBarras { get; set; } // Cambiado a nullable

        [MaxLength(500)]
        public string? TallaCamiseta { get; set; } // Añadido

        [MaxLength(1000)]
        public string? Observaciones { get; set; } // Añadido

        public EstadoAbonado Estado { get; set; } = EstadoAbonado.Activo; // Cambiado default
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
        [NotMapped] public string NombreCompleto => $"{Nombre} {Apellidos}";
        [NotMapped] public string EstadoTexto => Estado.ToString();
        [NotMapped]
        public string ColorEstado => Estado switch
        {
            EstadoAbonado.Activo => "#4CAF50",
            EstadoAbonado.Inactivo => "#FF9800",
            _ => "#9E9E9E"
        };
    }

    // También actualiza el enum de EstadoAbonado:
    public enum EstadoAbonado
    {
        Activo = 1,
        Inactivo = 0,
    }

    public class Gestor
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Nombre { get; set; } = "";
        [MaxLength(20)] public string? Telefono { get; set; } // Añadido
        [MaxLength(100)] public string? Email { get; set; } // Añadido
        [MaxLength(500)] public string? Observaciones { get; set; } // Añadido
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }
    public class Peña
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Nombre { get; set; } = "";
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }

    public class TipoAbono
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Nombre { get; set; } = "";
        [MaxLength(500)] public string? Descripcion { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal Precio { get; set; }
        public bool Activo { get; set; } = true; // Añadido
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public virtual ICollection<Abonado> Abonados { get; set; } = new List<Abonado>();
    }
    public class Usuario
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string NombreUsuario { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        [MaxLength(100)] public string? NombreCompleto { get; set; }
        [MaxLength(100)] public string? Email { get; set; }
        [Required, MaxLength(20)] public string Rol { get; set; } = "Usuario";
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? UltimoAcceso { get; set; }

        public virtual UserPermissions? Permissions { get; set; }
        public virtual ICollection<HistorialAccion> HistorialAcciones { get; set; } = new List<HistorialAccion>();
    }

    public class UserPermissions
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;

        // Permisos de acceso
        public bool CanAccessDashboard { get; set; } = true;
        public bool CanAccessAbonados { get; set; } = false;
        public bool CanAccessTiposAbono { get; set; } = false;
        public bool CanAccessGestores { get; set; } = false;
        public bool CanAccessPeñas { get; set; } = false;
        public bool CanAccessUsuarios { get; set; } = false;
        public bool CanAccessHistorial { get; set; } = false;
        public bool CanAccessConfiguracion { get; set; } = false;
        public bool CanAccessTemplates { get; set; } = false; // Añadido

        // Permisos de acción
        public bool CanEdit { get; set; } = false; // Añadido
        public bool CanDelete { get; set; } = false; // Añadido
        public bool CanExport { get; set; } = false; // Añadido
        public bool CanPrint { get; set; } = false; // Añadido
        public bool CanCreateAbonados { get; set; } = false;
        public bool CanEditAbonados { get; set; } = false;
        public bool CanDeleteAbonados { get; set; } = false;
        public bool CanExportData { get; set; } = false;
        public bool CanImportData { get; set; } = false;
        public bool CanManageSeasons { get; set; } = false;
        public bool CanChangeLicense { get; set; } = false;
        public bool CanCreateBackups { get; set; } = false;
    }

    public class HistorialAccion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;
        [Required, MaxLength(200)] public string Accion { get; set; } = "";
        [MaxLength(50)] public string TipoAccion { get; set; } = "";
        public DateTime FechaHora { get; set; } = DateTime.Now;
        [MaxLength(500)] public string? Detalles { get; set; }
    }

    // SESIÓN DE USUARIO (SINGLETON)
    public sealed class UserSession : INotifyPropertyChanged
    {
        private static UserSession? _instance;
        private Usuario? _currentUser;
        private UserPermissions? _currentPermissions;

        public static UserSession Instance => _instance ??= new UserSession();

        public bool IsLoggedIn => _currentUser != null;
        public Usuario? CurrentUser
        {
            get => _currentUser;
            private set { _currentUser = value; OnPropertyChanged(); }
        }

        public UserPermissions? CurrentPermissions
        {
            get => _currentPermissions;
            private set { _currentPermissions = value; OnPropertyChanged(); }
        }

        public void Login(Usuario user, UserPermissions permissions)
        {
            CurrentUser = user;
            CurrentPermissions = permissions;
            user.UltimoAcceso = DateTime.Now;
        }

        public void Logout()
        {
            CurrentUser = null;
            CurrentPermissions = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // SERVICIOS DE LICENCIA
    public interface ILicenseService
    {
        LicenseInfo GetCurrentLicenseInfo();
        bool ActivateLicense(string licenseKey);
        void LoadSavedLicense();
    }

    public class LicenseInfo
    {
        /// <summary>
        /// Indica si la licencia es válida
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Indica si la licencia ha expirado
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Nombre del club de la licencia
        /// </summary>
        public string ClubName { get; set; } = "";

        /// <summary>
        /// Fecha de expiración de la licencia
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Estado actual de la licencia (texto descriptivo)
        /// </summary>
        public string Status { get; set; } = "";

        /// <summary>
        /// Mensaje de error si la licencia no es válida
        /// </summary>
        public string ErrorMessage { get; set; } = "";

        /// <summary>
        /// Tiempo restante antes de la expiración (texto formateado)
        /// </summary>
        public string TimeRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                    return "Sin límite";

                if (IsExpired)
                    return "Expirada";

                TimeSpan remaining = ExpirationDate.Value - DateTime.Now;

                if (remaining.TotalDays > 365)
                    return $"{(int)(remaining.TotalDays / 365)} año(s)";
                else if (remaining.TotalDays > 30)
                    return $"{(int)(remaining.TotalDays / 30)} mes(es)";
                else if (remaining.TotalDays > 1)
                    return $"{(int)remaining.TotalDays} día(s)";
                else if (remaining.TotalHours > 1)
                    return $"{(int)remaining.TotalHours} hora(s)";
                else
                    return $"{(int)remaining.TotalMinutes} minuto(s)";
            }
        }

        /// <summary>
        /// Fecha de expiración formateada
        /// </summary>
        public string ExpirationText => ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Sin límite";
    }

    // PLANTILLAS DE TARJETAS
    // Reemplazar las clases de ElementoTarjeta en tu Models.cs por estas versiones actualizadas:

    // PLANTILLAS DE TARJETAS
    public class PlantillaTarjeta
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public double Ancho { get; set; } = 350;
        public double Alto { get; set; } = 220;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
        public List<ElementoTarjeta> Elementos { get; set; } = new();
    }

    public abstract class ElementoTarjeta
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Ancho { get; set; }
        public double Alto { get; set; }
        public int ZIndex { get; set; }

        // Propiedad virtual que las clases derivadas pueden sobreescribir
        public virtual string Tipo => GetType().Name.Replace("Elemento", "");
    }

    public class ElementoTexto : ElementoTarjeta
    {
        public string Texto { get; set; } = "";
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.Black;
        public string TextAlignment { get; set; } = "Left";

        public override string Tipo => "Texto";
    }

    public class ElementoImagen : ElementoTarjeta
    {
        public string RutaImagen { get; set; } = "";
        public string Stretch { get; set; } = "Uniform";

        public override string Tipo => "Imagen";
    }

    public class ElementoCodigo : ElementoTarjeta
    {
        public string TipoCodigo { get; set; } = "CodigoBarras";
        public string CampoOrigen { get; set; } = "CodigoBarras";

        public override string Tipo => "Código";
    }

    // Nueva clase para campos dinámicos
    public class ElementoCampoDinamico : ElementoTarjeta
    {
        public string Texto { get; set; } = "";
        public string CampoOrigen { get; set; } = "";
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.Black;
        public string TextAlignment { get; set; } = "Left";

        public override string Tipo => "Campo Dinámico";
    }
    public class Configuracion
    {
        public int Id { get; set; }
        public string NombreClub { get; set; } = "";
        public string TemporadaActual { get; set; } = "";
        public string DireccionClub { get; set; } = "";
        public string TelefonoClub { get; set; } = "";
        public string EmailClub { get; set; } = "";
        public string WebClub { get; set; } = "";
        public string LogoClub { get; set; } = "";
        public string RutaEscudo { get; set; } = ""; // Añadido
        public bool AutoBackup { get; set; } = true;
        public bool ConfirmarEliminaciones { get; set; } = true;
        public bool MostrarAyudas { get; set; } = true;
        public bool NumeracionAutomatica { get; set; } = true;
        public string FormatoNumeroSocio { get; set; } = "simple";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
    }
}