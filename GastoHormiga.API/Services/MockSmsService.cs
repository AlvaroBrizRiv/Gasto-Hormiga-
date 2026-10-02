namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Implementación MOCK del servicio de SMS.
    /// En lugar de enviar un SMS real, registra el código OTP en los logs del servidor.
    ///
    /// ✅ OPEN SOURCE READY: Esta implementación permite usar la app sin costo ni cuenta de SMS.
    /// Para ver el código OTP durante desarrollo, revisa la consola del servidor API.
    ///
    /// 🔄 PARA PRODUCCIÓN: Reemplaza esta clase por TwilioSmsService o AwsSnsService
    /// implementando la misma interfaz ISmsService, sin cambiar ningún otro archivo.
    /// </summary>
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarOtpAsync(string telefono, string codigoOtp)
        {
            // ═══════════════════════════════════════════════════════════════════
            // MODO MOCK: El código OTP se muestra en los logs del servidor.
            // En un entorno de producción, reemplaza esta implementación por
            // un servicio SMS real (Twilio, AWS SNS, etc.).
            // ═══════════════════════════════════════════════════════════════════
            _logger.LogWarning("═══════════════════════════════════════════════════");
            _logger.LogWarning("📱 [SMS MOCK] Destinatario: {Telefono}", telefono);
            _logger.LogWarning("🔑 [SMS MOCK] Código OTP: {Codigo}", codigoOtp);
            _logger.LogWarning("⏱️  [SMS MOCK] Válido por 10 minutos");
            _logger.LogWarning("═══════════════════════════════════════════════════");

            // Simula éxito del envío
            return Task.FromResult(true);
        }
    }
}
