using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.Model
{
    /// <summary>
    /// Entidad de dominio Usuario. Encapsula toda la lógica de negocio
    /// relacionada con el usuario, incluyendo verificación OTP para el registro.
    /// Sigue el principio de encapsulamiento (SOLID-S) con setters privados.
    /// </summary>
    public class Usuario
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; private set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido { get; private set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; private set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Telefono { get; private set; } = string.Empty;

        /// <summary>Código de país ISO 3166-1 alpha-2 (ej: CL, AR, MX, US)</summary>
        [Required]
        [MaxLength(5)]
        public string CodigoPais { get; private set; } = "CL";

        /// <summary>Código ISO 4217 de la divisa (ej: CLP, ARS, USD)</summary>
        [Required]
        [MaxLength(10)]
        public string DivisaCodigo { get; private set; } = "CLP";

        /// <summary>Hash de la contraseña usando BCrypt</summary>
        [Required]
        public string PasswordHash { get; private set; } = string.Empty;

        public bool EmailVerificado { get; private set; } = false;
        public bool TelefonoVerificado { get; private set; } = false;

        /// <summary>Código OTP de 6 dígitos enviado por SMS</summary>
        [MaxLength(6)]
        public string? CodigoOtp { get; private set; }

        /// <summary>Fecha/hora de expiración del OTP (10 minutos)</summary>
        public DateTime? OtpExpiracion { get; private set; }

        public DateTime FechaRegistro { get; private set; } = DateTime.UtcNow;
        public bool Activo { get; private set; } = true;

        public ICollection<Suscripcion> Suscripciones { get; private set; } = new List<Suscripcion>();

        /// <summary>Reglas de clasificación personalizadas por el usuario (motor Human-in-the-Loop)</summary>
        public ICollection<ReglaMercante> ReglasMercante { get; private set; } = new List<ReglaMercante>();

        /// <summary>Alertas de patrones de gasto generadas por el clasificador</summary>
        public ICollection<AlertaPatron> AlertasPatron { get; private set; } = new List<AlertaPatron>();

        /// <summary>
        /// Constructor principal de dominio con validaciones de negocio.
        /// </summary>
        public Usuario(string nombre, string apellido, string email, string telefono,
                       string passwordHash, string codigoPais = "CL", string divisaCodigo = "CLP")
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(apellido))
                throw new ArgumentException("El apellido es obligatorio.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.");
            if (string.IsNullOrWhiteSpace(telefono))
                throw new ArgumentException("El teléfono es obligatorio.");
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("El hash de contraseña es obligatorio.");

            Nombre = nombre.Trim();
            Apellido = apellido.Trim();
            Email = email.Trim().ToLowerInvariant();
            Telefono = telefono.Trim();
            PasswordHash = passwordHash;
            CodigoPais = codigoPais.ToUpperInvariant();
            DivisaCodigo = divisaCodigo.ToUpperInvariant();
            FechaRegistro = DateTime.UtcNow;
        }

        /// <summary>
        /// Genera y almacena un nuevo código OTP con expiración de 10 minutos.
        /// </summary>
        public void GenerarNuevoOtp()
        {
            var random = new Random();
            CodigoOtp = random.Next(100000, 999999).ToString();
            OtpExpiracion = DateTime.UtcNow.AddMinutes(10);
            TelefonoVerificado = false;
        }

        /// <summary>
        /// Verifica si el código OTP proporcionado es válido y no ha expirado.
        /// </summary>
        public bool VerificarOtp(string codigo)
        {
            if (CodigoOtp == null || OtpExpiracion == null)
                return false;
            if (DateTime.UtcNow > OtpExpiracion)
                return false;
            if (CodigoOtp != codigo)
                return false;

            TelefonoVerificado = true;
            CodigoOtp = null;
            OtpExpiracion = null;
            return true;
        }

        /// <summary>
        /// Marca el email como verificado.
        /// </summary>
        public void VerificarEmail() => EmailVerificado = true;

        /// <summary>
        /// Constructor vacío requerido por Entity Framework Core.
        /// No usar directamente en código de aplicación.
        /// </summary>
        protected Usuario() { }
    }
}
