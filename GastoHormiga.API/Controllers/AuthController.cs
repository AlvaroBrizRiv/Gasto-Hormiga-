using GastoHormiga.API.Services;
using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GastoHormiga.API.Controllers
{
    /// <summary>
    /// Controlador de autenticación: registro, verificación OTP y login.
    /// No requiere autorización previa (es el punto de entrada para nuevos usuarios).
    /// 
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Frontend (Blazor): Consumido por Login.razor y Registro.razor usando HttpClient.
    /// - Backend (Service): Llama a AuthService.cs para manejar el hash de contraseñas, validación y generación del token JWT.
    /// - Seguridad: Implementa prevención de fuerza bruta y valida la identidad (OWASP).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registra un nuevo usuario y envía un OTP por SMS.
        /// </summary>
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistroUsuarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var (exito, mensaje) = await _authService.RegistrarAsync(dto);

            if (!exito)
                return Conflict(new { mensaje });

            return Ok(new { mensaje });
        }

        /// <summary>
        /// Verifica el código OTP enviado por SMS para activar la cuenta.
        /// </summary>
        [HttpPost("verificar-otp")]
        public async Task<IActionResult> VerificarOtp([FromBody] VerificarOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var (exito, mensaje) = await _authService.VerificarOtpAsync(dto);

            if (!exito)
                return BadRequest(new { mensaje });

            return Ok(new { mensaje });
        }

        /// <summary>
        /// Reenvía un nuevo código OTP al teléfono del usuario.
        /// </summary>
        [HttpPost("reenviar-otp")]
        public async Task<IActionResult> ReenviarOtp([FromBody] ReenviarOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { mensaje = "El email es obligatorio." });

            var (exito, mensaje) = await _authService.ReenviarOtpAsync(request.Email);

            if (!exito)
                return BadRequest(new { mensaje });

            return Ok(new { mensaje });
        }

        /// <summary>
        /// Inicia sesión y devuelve un JWT válido.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var (exito, respuesta, mensaje) = await _authService.LoginAsync(dto);

            if (!exito)
                return Unauthorized(new { mensaje });

            return Ok(respuesta);
        }
    }

    public class ReenviarOtpRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
