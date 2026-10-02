using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GastoHormiga.API.Data;
using GastoHormiga.Shared.DTOs;
using GastoHormiga.Shared.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Servicio de autenticación: registro, verificación OTP y generación de JWT.
    /// Implementa OWASP A02 (BCrypt), A07 (JWT cortos, rate limiting externo),
    /// y la regla de negocio de unicidad de email y teléfono.
    /// Principio: Single Responsibility (SOLID-S).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            ISmsService smsService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _smsService = smsService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<(bool Exito, string Mensaje)> RegistrarAsync(RegistroUsuarioDto dto)
        {
            // Verificar unicidad de email (case-insensitive)
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == dto.Email.ToLowerInvariant());

            if (emailExiste)
                return (false, "Ya existe una cuenta registrada con ese correo electrónico.");

            // Verificar unicidad de teléfono
            var telefonoExiste = await _context.Usuarios
                .AnyAsync(u => u.Telefono == dto.Telefono);

            if (telefonoExiste)
                return (false, "Ya existe una cuenta registrada con ese número de teléfono.");

            // Hash de la contraseña con BCrypt (factor de costo 12 — OWASP A02)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            // Crear usuario con el constructor de dominio
            var nuevoUsuario = new Usuario(
                dto.Nombre,
                dto.Apellido,
                dto.Email,
                dto.Telefono,
                passwordHash,
                dto.CodigoPais,
                dto.DivisaCodigo
            );

            // Generar OTP y guardar usuario
            nuevoUsuario.GenerarNuevoOtp();

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // Enviar OTP por SMS (Mock en desarrollo)
            var telefonoCompleto = $"+{ObtenerPrefijo(dto.CodigoPais)}{dto.Telefono.TrimStart('0')}";
            var smsExitoso = await _smsService.EnviarOtpAsync(telefonoCompleto, nuevoUsuario.CodigoOtp!);

            if (!smsExitoso)
            {
                _logger.LogError("Error al enviar OTP al teléfono {Telefono}", dto.Telefono);
                // No revertimos el registro, el usuario puede pedir reenvío
            }

            _logger.LogInformation("Usuario registrado: {Email}", dto.Email);
            return (true, "Usuario registrado. Verifica tu teléfono con el código enviado por SMS.");
        }

        /// <inheritdoc/>
        public async Task<(bool Exito, string Mensaje)> VerificarOtpAsync(VerificarOtpDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant());

            if (usuario == null)
                return (false, "No se encontró una cuenta con ese correo.");

            if (usuario.TelefonoVerificado)
                return (true, "El teléfono ya fue verificado anteriormente.");

            // Verificación de OTP mediante método de dominio
            var otpValido = usuario.VerificarOtp(dto.CodigoOtp);

            if (!otpValido)
                return (false, "El código OTP es incorrecto o ha expirado. Solicita uno nuevo.");

            await _context.SaveChangesAsync();

            _logger.LogInformation("Teléfono verificado para: {Email}", dto.Email);
            return (true, "¡Teléfono verificado con éxito! Ya puedes iniciar sesión.");
        }

        /// <inheritdoc/>
        public async Task<(bool Exito, string Mensaje)> ReenviarOtpAsync(string email)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

            if (usuario == null)
                return (false, "No se encontró una cuenta con ese correo.");

            if (usuario.TelefonoVerificado)
                return (false, "El teléfono ya fue verificado.");

            // Generar nuevo OTP
            usuario.GenerarNuevoOtp();
            await _context.SaveChangesAsync();

            var telefonoCompleto = $"+{ObtenerPrefijo(usuario.CodigoPais)}{usuario.Telefono.TrimStart('0')}";
            await _smsService.EnviarOtpAsync(telefonoCompleto, usuario.CodigoOtp!);

            return (true, "Se envió un nuevo código OTP a tu teléfono.");
        }

        /// <inheritdoc/>
        public async Task<(bool Exito, AuthResponseDto? Respuesta, string Mensaje)> LoginAsync(LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant());

            // Respuesta genérica para no revelar si el email existe (OWASP A07)
            const string mensajeErrorGenerico = "Credenciales incorrectas. Verifica tu email y contraseña.";

            if (usuario == null)
                return (false, null, mensajeErrorGenerico);

            if (!usuario.Activo)
                return (false, null, "Tu cuenta está desactivada. Contacta soporte.");

            if (!usuario.TelefonoVerificado)
                return (false, null, "Debes verificar tu número de teléfono antes de iniciar sesión.");

            // Verificar contraseña con BCrypt (tiempo constante para prevenir timing attacks)
            var passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);

            if (!passwordValida)
                return (false, null, mensajeErrorGenerico);

            // Generar JWT
            var (token, expiracion) = GenerarToken(usuario);

            var respuesta = new AuthResponseDto
            {
                Token = token,
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                DivisaCodigo = usuario.DivisaCodigo,
                CodigoPais = usuario.CodigoPais,
                Expiracion = expiracion
            };

            _logger.LogInformation("Login exitoso: {Email}", dto.Email);
            return (true, respuesta, "Inicio de sesión exitoso.");
        }

        /// <summary>
        /// Genera un JWT con claims del usuario.
        /// Token válido por 8 horas (configurable).
        /// </summary>
        private (string Token, DateTime Expiracion) GenerarToken(Usuario usuario)
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
            var issuer = jwtConfig["Issuer"] ?? "GastoHormigaAPI";
            var audience = jwtConfig["Audience"] ?? "GastoHormigaWeb";
            var duracionHoras = int.Parse(jwtConfig["DuracionHoras"] ?? "8");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiracion = DateTime.UtcNow.AddHours(duracionHoras);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
                new Claim("divisa", usuario.DivisaCodigo),
                new Claim("pais", usuario.CodigoPais),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiracion,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiracion);
        }

        /// <summary>
        /// Devuelve el prefijo telefónico internacional según el código de país.
        /// Escalable: agrega más países en este método.
        /// </summary>
        private static string ObtenerPrefijo(string codigoPais) => codigoPais.ToUpperInvariant() switch
        {
            "CL" => "56",
            "AR" => "54",
            "MX" => "52",
            "PE" => "51",
            "CO" => "57",
            "BR" => "55",
            "US" => "1",
            "ES" => "34",
            _ => "56" // Default Chile
        };
    }
}
