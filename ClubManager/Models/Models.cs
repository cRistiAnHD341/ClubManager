using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

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

    /// </summary>
    public class Configuracion
    {
        // Clave primaria para Entity Framework
        public int Id { get; set; } = 1;

        // Información del club
        public string NombreClub { get; set; } = "Mi Club Deportivo";
        public string TemporadaActual { get; set; } = DateTime.Now.Year.ToString();
        public string DireccionClub { get; set; } = "";
        public string TelefonoClub { get; set; } = "";
        public string EmailClub { get; set; } = "";
        public string WebClub { get; set; } = "";
        public string LogoClub { get; set; } = "";
        public string RutaEscudo { get; set; } = "";

        // Configuración del sistema
        public bool AutoBackup { get; set; } = true;
        public bool ConfirmarEliminaciones { get; set; } = true;
        public bool MostrarAyudas { get; set; } = true;
        public bool NumeracionAutomatica { get; set; } = true;
        public string FormatoNumeroSocio { get; set; } = "###000";

        // NUEVA: Configuración de impresión de tarjetas
        public ConfiguracionImpresionTarjetas ConfiguracionImpresion { get; set; } = new();

        // Metadatos
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";

        // Formatos disponibles para número de socio
        public static List<FormatoNumeroSocioItem> FormatosDisponibles => new()
    {
        new FormatoNumeroSocioItem { Value = "000000", Display = "6 dígitos (001234)" },
        new FormatoNumeroSocioItem { Value = "###000", Display = "Prefijo + 3 dígitos (ABC123)" },
        new FormatoNumeroSocioItem { Value = "0000", Display = "4 dígitos (1234)" },
        new FormatoNumeroSocioItem { Value = "00000", Display = "5 dígitos (01234)" }
    };
    }
    public class ConfiguracionImpresionTarjetas
    {
        // Impresora
        public string ImpresoraPredeterminada { get; set; } = "";
        public List<string> ImpresorasDisponibles { get; set; } = new();

        // Configuración de página
        public string TamañoPapel { get; set; } = "A4";
        public string Orientacion { get; set; } = "Vertical"; // Vertical, Horizontal
        public bool ImpresionColor { get; set; } = true;
        public int Calidad { get; set; } = 300; // DPI

        // Márgenes (en milímetros)
        public double MargenSuperior { get; set; } = 10;
        public double MargenInferior { get; set; } = 10;
        public double MargenIzquierdo { get; set; } = 10;
        public double MargenDerecho { get; set; } = 10;

        // Distribución de tarjetas
        public int TarjetasPorPagina { get; set; } = 10;
        public double EspaciadoHorizontal { get; set; } = 5; // mm
        public double EspaciadoVertical { get; set; } = 5; // mm
        public bool AjustarTamañoAutomaticamente { get; set; } = true;

        // Opciones adicionales
        public bool MostrarBordeCorte { get; set; } = false;
        public bool ImprimirEnDuplicado { get; set; } = false;
        public bool MarcarComoImpresoAutomaticamente { get; set; } = true;
        public bool MostrarVistaPrevia { get; set; } = true;

        // Configuración de backup de impresión
        public bool GuardarCopiaDigital { get; set; } = false;
        public string RutaCopiasDigitales { get; set; } = "";

        // Configuración avanzada
        public bool UsarConfiguracionPersonalizada { get; set; } = false;
        public string ConfiguracionPersonalizadaJson { get; set; } = "";

        // Formatos de papel disponibles
        public static List<TamañoPapelItem> TamañosPapelDisponibles => new()
        {
            new TamañoPapelItem { Value = "A4", Display = "A4 (210 x 297 mm)", Ancho = 210, Alto = 297 },
            new TamañoPapelItem { Value = "A5", Display = "A5 (148 x 210 mm)", Ancho = 148, Alto = 210 },
            new TamañoPapelItem { Value = "Letter", Display = "Carta (216 x 279 mm)", Ancho = 216, Alto = 279 },
            new TamañoPapelItem { Value = "Legal", Display = "Legal (216 x 356 mm)", Ancho = 216, Alto = 356 },
            new TamañoPapelItem { Value = "Tarjeta", Display = "Tarjeta (86 x 54 mm)", Ancho = 86, Alto = 54 }
        };

        // Calidades disponibles
        public static List<CalidadImpresionItem> CalidadesDisponibles => new()
        {
            new CalidadImpresionItem { Value = 150, Display = "Borrador (150 DPI)" },
            new CalidadImpresionItem { Value = 300, Display = "Normal (300 DPI)" },
            new CalidadImpresionItem { Value = 600, Display = "Alta (600 DPI)" },
            new CalidadImpresionItem { Value = 1200, Display = "Muy Alta (1200 DPI)" }
        };
    }

    public class FormatoNumeroSocioItem
    {
        public string Value { get; set; } = "";
        public string Display { get; set; } = "";
    }

    /// <summary>
    /// Item para selector de tamaño de papel
    /// </summary>
    public class TamañoPapelItem
    {
        public string Value { get; set; } = "";
        public string Display { get; set; } = "";
        public double Ancho { get; set; }
        public double Alto { get; set; }
    }

    /// <summary>
    /// Item para selector de calidad de impresión
    /// </summary>
    public class CalidadImpresionItem
    {
        public int Value { get; set; }
        public string Display { get; set; } = "";
    }

    /// <summary>
    /// Configuración específica para una impresión
    /// </summary>
    public class ConfiguracionImpresionEspecifica
    {
        public string NombreImpresora { get; set; } = "";
        public string TamañoPapel { get; set; } = "A4";
        public bool ImpresionColor { get; set; } = true;
        public int Calidad { get; set; } = 300;
        public int Copias { get; set; } = 1;
        public bool MostrarDialogoImpresion { get; set; } = true;
        public bool GuardarCopia { get; set; } = false;
        public string RutaCopia { get; set; } = "";
    }

    /// <summary>
    /// Resultado de operación de impresión
    /// </summary>
    public class ResultadoImpresion
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = "";
        public int TarjetasImpresas { get; set; }
        public int TarjetasConError { get; set; }
        public List<string> Errores { get; set; } = new();
        public TimeSpan TiempoTranscurrido { get; set; }
        public DateTime FechaImpresion { get; set; } = DateTime.Now;
        public string ImpresoraUtilizada { get; set; } = "";
    }


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
        public bool EsPredeterminada { get; set; } = false;
    }

    /// <summary>
    /// Elemento base para todos los componentes de la tarjeta
    /// </summary>
    [JsonDerivedType(typeof(ElementoTexto), "texto")]
    [JsonDerivedType(typeof(ElementoImagen), "imagen")]
    [JsonDerivedType(typeof(ElementoCodigoBarras), "codigoBarras")]
    [JsonDerivedType(typeof(ElementoCampoDinamico), "campoDinamico")]
    public abstract class ElementoTarjeta
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Tipo { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Ancho { get; set; }
        public double Alto { get; set; }
        public int ZIndex { get; set; }
        public bool Visible { get; set; } = true;
        public double Rotacion { get; set; } = 0;
        public double Opacidad { get; set; } = 1.0;
    }

    /// <summary>
    /// Elemento de texto estático
    /// </summary>
    public class ElementoTexto : ElementoTarjeta
    {
        public string Texto { get; set; } = "";
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color Color { get; set; } = Colors.Black;

        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
        public bool AutoSize { get; set; } = false;
        public bool WordWrap { get; set; } = false;
    }

    /// <summary>
    /// Elemento de imagen
    /// </summary>
    public class ElementoImagen : ElementoTarjeta
    {
        public string RutaImagen { get; set; } = "";
        public bool MantenerAspecto { get; set; } = true;
        public double Redondez { get; set; } = 0; // Para bordes redondeados
        public bool EsLogo { get; set; } = false; // Indica si es el logo del club

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color ColorBorde { get; set; } = Colors.Transparent;

        public double GrosorBorde { get; set; } = 0;
    }

    /// <summary>
    /// Elemento de código de barras
    /// </summary>
    public class ElementoCodigoBarras : ElementoTarjeta
    {
        public string TipoCodigo { get; set; } = "Code128"; // Code128, Code39, QR, etc.
        public string CampoOrigen { get; set; } = "CodigoBarras"; // Campo del abonado
        public bool MostrarTexto { get; set; } = true;
        public string FontFamily { get; set; } = "Courier New";
        public double FontSize { get; set; } = 8;

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color ColorTexto { get; set; } = Colors.Black;

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color ColorFondo { get; set; } = Colors.White;

        public string FormatoTexto { get; set; } = ""; // Formato personalizado para el texto
    }

    /// <summary>
    /// Elemento de campo dinámico (datos del abonado)
    /// </summary>
    public class ElementoCampoDinamico : ElementoTarjeta
    {
        public string CampoOrigen { get; set; } = ""; // NombreCompleto, NumeroSocio, etc.
        public string Texto { get; set; } = ""; // Template del texto con placeholders
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color Color { get; set; } = Colors.Black;

        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
        public string Prefijo { get; set; } = ""; // Texto antes del valor
        public string Sufijo { get; set; } = ""; // Texto después del valor
        public string FormatoNumero { get; set; } = ""; // Para números: N0, C, etc.
        public string FormatoFecha { get; set; } = "dd/MM/yyyy"; // Para fechas
        public bool AutoSize { get; set; } = false;
        public string TextoSiVacio { get; set; } = ""; // Texto a mostrar si el campo está vacío
    }

    /// <summary>
    /// Configuración de impresión para la plantilla
    /// </summary>
    public class ConfiguracionImpresion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PlantillaId { get; set; } = "";
        public string NombreImpresora { get; set; } = "";
        public double MargenIzquierdo { get; set; } = 0;
        public double MargenSuperior { get; set; } = 0;
        public double MargenDerecho { get; set; } = 0;
        public double MargenInferior { get; set; } = 0;
        public int Calidad { get; set; } = 300; // DPI
        public bool ImpresionColor { get; set; } = true;
        public bool DoblesCara { get; set; } = false;
        public string TamañoPapel { get; set; } = "A4";
        public string Orientacion { get; set; } = "Vertical"; // Vertical, Horizontal
        public int TarjetasPorPagina { get; set; } = 10;
        public double EspaciadoHorizontal { get; set; } = 5;
        public double EspaciadoVertical { get; set; } = 5;
    }

    /// <summary>
    /// Historial de uso de plantillas
    /// </summary>
    public class HistorialPlantilla
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PlantillaId { get; set; } = "";
        public string AccionRealizada { get; set; } = ""; // Creada, Modificada, Utilizada, etc.
        public DateTime FechaAccion { get; set; } = DateTime.Now;
        public string UsuarioId { get; set; } = "";
        public string Detalles { get; set; } = "";
        public int AbonadosAfectados { get; set; } = 0;
    }

    /// <summary>
    /// Estadísticas de uso de plantillas
    /// </summary>
    public class EstadisticasPlantilla
    {
        public string PlantillaId { get; set; } = "";
        public string NombrePlantilla { get; set; } = "";
        public int VecesUtilizada { get; set; } = 0;
        public DateTime UltimaUtilizacion { get; set; }
        public int TarjetasImpresas { get; set; } = 0;
        public DateTime FechaCreacion { get; set; }
        public bool EstaActiva { get; set; } = true;
        public double TiempoPromedioImpresion { get; set; } = 0; // En segundos
    }
}

/// <summary>
/// Converter personalizado para serializar/deserializar colores
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var colorString = reader.GetString();
        if (string.IsNullOrEmpty(colorString))
            return Colors.Black;

        try
        {
            // Soportar diferentes formatos de color
            if (colorString.StartsWith("#"))
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            else if (colorString.Contains(","))
            {
                // Formato R,G,B,A
                var parts = colorString.Split(',');
                if (parts.Length >= 3)
                {
                    var r = byte.Parse(parts[0]);
                    var g = byte.Parse(parts[1]);
                    var b = byte.Parse(parts[2]);
                    var a = parts.Length > 3 ? byte.Parse(parts[3]) : (byte)255;
                    return Color.FromArgb(a, r, g, b);
                }
            }
            else
            {
                // Nombres de colores
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
        }
        catch
        {
            return Colors.Black;
        }

        return Colors.Black;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}

/// <summary>
/// Extensiones útiles para trabajar con plantillas
/// </summary>
public static class PlantillaExtensions
{
    /// <summary>
    /// Clona una plantilla con un nuevo ID
    /// </summary>
    public static PlantillaTarjeta Clonar(this PlantillaTarjeta plantilla, string? nuevoNombre = null)
    {
        var nuevaPlantilla = new PlantillaTarjeta
        {
            Id = Guid.NewGuid().ToString(),
            Nombre = nuevoNombre ?? $"{plantilla.Nombre} (Copia)",
            Descripcion = plantilla.Descripcion,
            Ancho = plantilla.Ancho,
            Alto = plantilla.Alto,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now,
            EsPredeterminada = false,
            Elementos = new List<ElementoTarjeta>()
        };

        foreach (var elemento in plantilla.Elementos)
        {
            nuevaPlantilla.Elementos.Add(elemento.Clonar());
        }

        return nuevaPlantilla;
    }

    /// <summary>
    /// Clona un elemento con un nuevo ID
    /// </summary>
    public static ElementoTarjeta Clonar(this ElementoTarjeta elemento)
    {
        return elemento switch
        {
            ElementoTexto texto => new ElementoTexto
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = texto.Tipo,
                X = texto.X,
                Y = texto.Y,
                Ancho = texto.Ancho,
                Alto = texto.Alto,
                ZIndex = texto.ZIndex,
                Visible = texto.Visible,
                Rotacion = texto.Rotacion,
                Opacidad = texto.Opacidad,
                Texto = texto.Texto,
                FontFamily = texto.FontFamily,
                FontSize = texto.FontSize,
                Color = texto.Color,
                IsBold = texto.IsBold,
                IsItalic = texto.IsItalic,
                IsUnderline = texto.IsUnderline,
                TextAlignment = texto.TextAlignment,
                AutoSize = texto.AutoSize,
                WordWrap = texto.WordWrap
            },

            ElementoImagen imagen => new ElementoImagen
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = imagen.Tipo,
                X = imagen.X,
                Y = imagen.Y,
                Ancho = imagen.Ancho,
                Alto = imagen.Alto,
                ZIndex = imagen.ZIndex,
                Visible = imagen.Visible,
                Rotacion = imagen.Rotacion,
                Opacidad = imagen.Opacidad,
                RutaImagen = imagen.RutaImagen,
                MantenerAspecto = imagen.MantenerAspecto,
                Redondez = imagen.Redondez,
                EsLogo = imagen.EsLogo,
                ColorBorde = imagen.ColorBorde,
                GrosorBorde = imagen.GrosorBorde
            },

            ElementoCodigoBarras codigo => new ElementoCodigoBarras
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = codigo.Tipo,
                X = codigo.X,
                Y = codigo.Y,
                Ancho = codigo.Ancho,
                Alto = codigo.Alto,
                ZIndex = codigo.ZIndex,
                Visible = codigo.Visible,
                Rotacion = codigo.Rotacion,
                Opacidad = codigo.Opacidad,
                TipoCodigo = codigo.TipoCodigo,
                CampoOrigen = codigo.CampoOrigen,
                MostrarTexto = codigo.MostrarTexto,
                FontFamily = codigo.FontFamily,
                FontSize = codigo.FontSize,
                ColorTexto = codigo.ColorTexto,
                ColorFondo = codigo.ColorFondo,
                FormatoTexto = codigo.FormatoTexto
            },

            ElementoCampoDinamico campo => new ElementoCampoDinamico
            {
                Id = Guid.NewGuid().ToString(),
                Tipo = campo.Tipo,
                X = campo.X,
                Y = campo.Y,
                Ancho = campo.Ancho,
                Alto = campo.Alto,
                ZIndex = campo.ZIndex,
                Visible = campo.Visible,
                Rotacion = campo.Rotacion,
                Opacidad = campo.Opacidad,
                CampoOrigen = campo.CampoOrigen,
                Texto = campo.Texto,
                FontFamily = campo.FontFamily,
                FontSize = campo.FontSize,
                Color = campo.Color,
                IsBold = campo.IsBold,
                IsItalic = campo.IsItalic,
                IsUnderline = campo.IsUnderline,
                TextAlignment = campo.TextAlignment,
                Prefijo = campo.Prefijo,
                Sufijo = campo.Sufijo,
                FormatoNumero = campo.FormatoNumero,
                FormatoFecha = campo.FormatoFecha,
                AutoSize = campo.AutoSize,
                TextoSiVacio = campo.TextoSiVacio
            },

            _ => throw new ArgumentException($"Tipo de elemento no soportado: {elemento.GetType()}")
        };
    }

    /// <summary>
    /// Valida que la plantilla esté correctamente configurada
    /// </summary>
    public static List<string> Validar(this PlantillaTarjeta plantilla)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(plantilla.Nombre))
            errores.Add("El nombre de la plantilla es requerido");

        if (plantilla.Ancho <= 0)
            errores.Add("El ancho debe ser mayor a 0");

        if (plantilla.Alto <= 0)
            errores.Add("El alto debe ser mayor a 0");

        if (plantilla.Ancho > 1000)
            errores.Add("El ancho no puede exceder 1000 píxeles");

        if (plantilla.Alto > 1000)
            errores.Add("El alto no puede exceder 1000 píxeles");

        // Validar elementos
        for (int i = 0; i < plantilla.Elementos.Count; i++)
        {
            var elemento = plantilla.Elementos[i];
            var erroresElemento = elemento.Validar();

            foreach (var error in erroresElemento)
            {
                errores.Add($"Elemento {i + 1}: {error}");
            }
        }

        return errores;
    }

    /// <summary>
    /// Valida un elemento individual
    /// </summary>
    public static List<string> Validar(this ElementoTarjeta elemento)
    {
        var errores = new List<string>();

        if (elemento.Ancho <= 0)
            errores.Add("El ancho debe ser mayor a 0");

        if (elemento.Alto <= 0)
            errores.Add("El alto debe ser mayor a 0");

        if (elemento.X < 0)
            errores.Add("La posición X no puede ser negativa");

        if (elemento.Y < 0)
            errores.Add("La posición Y no puede ser negativa");

        if (elemento.Opacidad < 0 || elemento.Opacidad > 1)
            errores.Add("La opacidad debe estar entre 0 y 1");

        // Validaciones específicas por tipo
        switch (elemento)
        {
            case ElementoTexto texto:
                if (string.IsNullOrWhiteSpace(texto.Texto))
                    errores.Add("El texto no puede estar vacío");
                if (texto.FontSize <= 0)
                    errores.Add("El tamaño de fuente debe ser mayor a 0");
                break;

            case ElementoImagen imagen:
                if (string.IsNullOrWhiteSpace(imagen.RutaImagen))
                    errores.Add("La ruta de imagen es requerida");
                else if (!File.Exists(imagen.RutaImagen))
                    errores.Add("El archivo de imagen no existe");
                break;

            case ElementoCodigoBarras codigo:
                if (string.IsNullOrWhiteSpace(codigo.CampoOrigen))
                    errores.Add("El campo origen del código de barras es requerido");
                if (string.IsNullOrWhiteSpace(codigo.TipoCodigo))
                    errores.Add("El tipo de código es requerido");
                break;

            case ElementoCampoDinamico campo:
                if (string.IsNullOrWhiteSpace(campo.CampoOrigen))
                    errores.Add("El campo origen es requerido");
                if (campo.FontSize <= 0)
                    errores.Add("El tamaño de fuente debe ser mayor a 0");
                break;
        }

        return errores;
    }

    /// <summary>
    /// Obtiene todos los campos dinámicos utilizados en la plantilla
    /// </summary>
    public static List<string> ObtenerCamposUtilizados(this PlantillaTarjeta plantilla)
    {
        var campos = new HashSet<string>();

        foreach (var elemento in plantilla.Elementos)
        {
            switch (elemento)
            {
                case ElementoCodigoBarras codigo:
                    if (!string.IsNullOrWhiteSpace(codigo.CampoOrigen))
                        campos.Add(codigo.CampoOrigen);
                    break;

                case ElementoCampoDinamico campo:
                    if (!string.IsNullOrWhiteSpace(campo.CampoOrigen))
                        campos.Add(campo.CampoOrigen);
                    break;
            }
        }

        return campos.ToList();
    }

    /// <summary>
    /// Verifica si la plantilla tiene elementos que requieren datos del abonado
    /// </summary>
    public static bool RequiereDatosAbonado(this PlantillaTarjeta plantilla)
    {
        return plantilla.Elementos.Any(e =>
            e is ElementoCodigoBarras ||
            e is ElementoCampoDinamico);
    }

    /// <summary>
    /// Calcula el área ocupada por todos los elementos
    /// </summary>
    public static (double minX, double minY, double maxX, double maxY) CalcularAreaOcupada(this PlantillaTarjeta plantilla)
    {
        if (!plantilla.Elementos.Any())
            return (0, 0, 0, 0);

        var minX = plantilla.Elementos.Min(e => e.X);
        var minY = plantilla.Elementos.Min(e => e.Y);
        var maxX = plantilla.Elementos.Max(e => e.X + e.Ancho);
        var maxY = plantilla.Elementos.Max(e => e.Y + e.Alto);

        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Optimiza la distribución de elementos para evitar solapamientos
    /// </summary>
    public static void OptimizarDistribucion(this PlantillaTarjeta plantilla)
    {
        var elementos = plantilla.Elementos.OrderBy(e => e.Y).ThenBy(e => e.X).ToList();

        for (int i = 0; i < elementos.Count; i++)
        {
            var elemento = elementos[i];

            // Verificar solapamiento con elementos anteriores
            for (int j = 0; j < i; j++)
            {
                var otro = elementos[j];

                if (HaySolapamiento(elemento, otro))
                {
                    // Mover elemento para evitar solapamiento
                    elemento.Y = otro.Y + otro.Alto + 5; // 5px de margen
                }
            }
        }
    }

    private static bool HaySolapamiento(ElementoTarjeta elem1, ElementoTarjeta elem2)
    {
        return !(elem1.X + elem1.Ancho <= elem2.X ||
                 elem2.X + elem2.Ancho <= elem1.X ||
                 elem1.Y + elem1.Alto <= elem2.Y ||
                 elem2.Y + elem2.Alto <= elem1.Y);
    }

}
