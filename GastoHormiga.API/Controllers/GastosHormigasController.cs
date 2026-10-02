using System.Security.Claims;
using GastoHormiga.API.Services;
using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastoHormiga.API.Controllers
{
    /// <summary>
    /// Controlador de gastos hormiga. Requiere autenticación JWT.
    /// El UsuarioId se extrae del token JWT (no del body) para prevenir IDOR (OWASP A01).
    /// 
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Frontend (Blazor): Consumido por Dashboard.razor y MisGastos.razor a través de HttpClient.
    /// - Backend (Service): Inyecta IGastoHormigaService para delegar la lógica de negocio real.
    /// - Retorno: Devuelve objetos JSON mapeados desde GastoHormigaResponseDto.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GastosHormigaController : ControllerBase
    {
        private readonly IGastoHormigaService _service;
        private readonly IClasificadorService _clasificador;

        public GastosHormigaController(IGastoHormigaService service, IClasificadorService clasificador)
        {
            _service = service;
            _clasificador = clasificador;
        }

        /// <summary>
        /// Obtiene todos los gastos hormiga del usuario autenticado.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerMisGastos()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var gastos = await _service.ObtenerPorUsuarioAsync(usuarioId.Value);
            return Ok(gastos);
        }

        /// <summary>
        /// Crea un nuevo gasto hormiga para el usuario autenticado.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GastoHormigaCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _service.CrearAsync(dto, usuarioId.Value);
            if (resultado == null)
                return BadRequest(new { mensaje = "No se pudo registrar el gasto." });

            // Ejecutar detección de patrones en background después de cada gasto guardado.
            // Si hay 3+ transacciones similares en 7 días, se crea una alerta para el usuario.
            _ = Task.Run(() => _clasificador.EjecutarDeteccionDePatronesAsync(usuarioId.Value));

            return CreatedAtAction(nameof(ObtenerMisGastos), resultado);
        }

        /// <summary>
        /// Elimina un gasto hormiga. Solo si pertenece al usuario autenticado.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var eliminado = await _service.EliminarAsync(id, usuarioId.Value);

            if (!eliminado)
                return NotFound(new { mensaje = "Gasto no encontrado o no tienes permiso para eliminarlo." });

            return NoContent();
        }

        /// <summary>
        /// Devuelve el resumen financiero (semanal, mensual, anual, ahorro potencial).
        /// </summary>
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumen()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            // Obtener código de divisa del JWT claim
            var divisaCodigo = User.FindFirst("divisa")?.Value ?? "CLP";

            var resumen = await _service.ObtenerResumenAsync(usuarioId.Value, divisaCodigo);
            return Ok(resumen);
        }

        /// <summary>
        /// Extrae el ID del usuario autenticado desde los claims del JWT.
        /// Previene IDOR — el usuario no puede falsificar su propio ID.
        /// </summary>
        private int? ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var id))
                return null;

            return id;
        }
    }
}
