using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.DTOs
{
    /// <summary>
    /// DTO para el registro de un nuevo usuario.
    /// Incluye validaciones de entrada para proteger contra datos malformados (OWASP A03).
    /// </summary>
    public class RegistroUsuarioDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El apellido no puede superar 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        [MaxLength(200, ErrorMessage = "El email no puede superar 200 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        [MaxLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres.")]
        public string Telefono { get; set; } = string.Empty;

        /// <summary>Código de país ISO 3166-1 alpha-2 (ej: CL, AR, MX)</summary>
        [Required(ErrorMessage = "El código de país es obligatorio.")]
        [MaxLength(5)]
        public string CodigoPais { get; set; } = "CL";

        /// <summary>Código ISO 4217 de la divisa (ej: CLP, ARS, USD)</summary>
        [Required(ErrorMessage = "La divisa es obligatoria.")]
        [MaxLength(10)]
        public string DivisaCodigo { get; set; } = "CLP";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
