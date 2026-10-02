using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.DTOs
{
    /// <summary>
    /// DTO para verificar el código OTP enviado por SMS al teléfono del usuario.
    /// </summary>
    public class VerificarOtpDto
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código OTP es obligatorio.")]
        [MinLength(6, ErrorMessage = "El código debe tener 6 dígitos.")]
        [MaxLength(6, ErrorMessage = "El código debe tener 6 dígitos.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe ser de 6 dígitos numéricos.")]
        public string CodigoOtp { get; set; } = string.Empty;
    }
}
