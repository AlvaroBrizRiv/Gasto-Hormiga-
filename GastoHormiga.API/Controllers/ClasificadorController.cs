using System.Security.Claims;
using GastoHormiga.API.Services;
using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastoHormiga.API.Controllers
{
    /// <summary>
    /// Controlador del motor de clasificación híbrido.
    /// Expone los 4 endpoints del sistema Human-in-the-Loop para el frontend.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Frontend (Blazor): Consumido por Dashboard.razor.cs vía HttpClient
    /// - Backend (Service): Delega toda la lógica a IClasificadorService → ClasificadorService
    /// - Seguridad: Todos los endpoints requieren JWT válido. El UsuarioId se extrae del token (no del body).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClasificadorController : ControllerBase
    {
        private readonly IClasificadorService _clasificador;

        public ClasificadorController(IClasificadorService clasificador)
        {
            _clasificador = clasificador;
        }

        /// <summary>
        /// Obtiene la sugerencia de categoría para un concepto/comercio dado.
        /// Se llama en tiempo real mientras el usuario escribe el nombre del gasto.
        /// </summary>
        /// <param name="concepto">Texto del concepto o nombre del comercio</param>
        /// <returns>SugerenciaClasificacionDto con categoría, confianza y origen de la regla</returns>
        [HttpGet("sugerencia")]
        public async Task<IActionResult> ObtenerSugerencia([FromQuery] string concepto)
        {
            if (string.IsNullOrWhiteSpace(concepto))
                return BadRequest(new { mensaje = "El concepto es obligatorio." });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var sugerencia = await _clasificador.SugerirClasificacionAsync(concepto, usuarioId.Value);
            return Ok(sugerencia);
        }

        /// <summary>
        /// Retorna las alertas de patrones pendientes de resolución para el usuario autenticado.
        /// Se consume al cargar el Dashboard para mostrar los banners de alerta.
        /// </summary>
        [HttpGet("alertas")]
        public async Task<IActionResult> ObtenerAlertas()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var alertas = await _clasificador.ObtenerAlertasPendientesAsync(usuarioId.Value);
            return Ok(alertas);
        }

        /// <summary>
        /// Guarda la decisión del usuario sobre si un comercio es "Presupuestado" o "Hormiga".
        /// Crea o actualiza la ReglaMercante permanente del usuario.
        /// </summary>
        [HttpPost("presupuestado")]
        public async Task<IActionResult> MarcarComercio([FromBody] MarcarPresupuestadoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            await _clasificador.MarcarComercioAsync(dto, usuarioId.Value);
            return Ok(new { mensaje = "Regla guardada correctamente." });
        }

        /// <summary>
        /// Resuelve una alerta de patrón con la decisión del usuario
        /// (aceptar como hormiga o marcar como presupuestado).
        /// </summary>
        /// <param name="id">ID de la alerta a resolver</param>
        [HttpPut("alertas/{id:int}/resolver")]
        public async Task<IActionResult> ResolverAlerta(int id, [FromBody] ResolverAlertaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resuelto = await _clasificador.ResolverAlertaAsync(id, dto, usuarioId.Value);

            if (!resuelto)
                return NotFound(new { mensaje = "Alerta no encontrada o no tienes permiso para resolverla." });

            return Ok(new { mensaje = "Alerta resuelta correctamente." });
        }

        /// <summary>
        /// Extrae el UsuarioId del JWT claim. Previene IDOR — el usuario no puede falsificar su ID.
        /// </summary>
        private int? ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null || !int.TryParse(claim.Value, out var id))
                return null;
            return id;
        }
    }
}
