using System;

namespace GastoHormiga.Shared.DTOs
{
    /// <summary>
    /// Respuesta devuelta al cliente tras un login exitoso.
    /// Contiene el JWT y los datos básicos del usuario para la sesión.
    /// </summary>
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DivisaCodigo { get; set; } = "CLP";
        public string CodigoPais { get; set; } = "CL";
        public DateTime Expiracion { get; set; }
    }
}
