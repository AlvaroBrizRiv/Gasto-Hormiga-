namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Contrato para el servicio de envío de SMS.
    /// Permite cambiar la implementación (Mock → Twilio → AWS SNS) sin tocar el código que lo consume.
    /// Principio: Interface Segregation + Dependency Inversion (SOLID-I, SOLID-D).
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Envía un código OTP al número de teléfono indicado.
        /// </summary>
        /// <param name="telefono">Número de teléfono con código de país (ej: +56912345678)</param>
        /// <param name="codigoOtp">Código de 6 dígitos a enviar</param>
        /// <returns>True si el envío fue exitoso</returns>
        Task<bool> EnviarOtpAsync(string telefono, string codigoOtp);
    }
}
