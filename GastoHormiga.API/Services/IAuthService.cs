using GastoHormiga.Shared.DTOs;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Contrato del servicio de autenticación.
    /// Principio: Interface Segregation (SOLID-I).
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Registra un nuevo usuario y envía OTP por SMS.</summary>
        Task<(bool Exito, string Mensaje)> RegistrarAsync(RegistroUsuarioDto dto);

        /// <summary>Verifica el OTP enviado por SMS y activa la cuenta.</summary>
        Task<(bool Exito, string Mensaje)> VerificarOtpAsync(VerificarOtpDto dto);

        /// <summary>Reenvía un nuevo OTP al teléfono del usuario.</summary>
        Task<(bool Exito, string Mensaje)> ReenviarOtpAsync(string email);

        /// <summary>Autentica al usuario y devuelve el JWT.</summary>
        Task<(bool Exito, AuthResponseDto? Respuesta, string Mensaje)> LoginAsync(LoginDto dto);
    }
}
