using System.Security.Claims;
using GastoHormiga.API.Services;
using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastoHormiga.API.Controllers
{
    /// <summary>
    /// Controlador de suscripciones. Requiere autenticación JWT.
    /// Protege contra IDOR extrayendo UsuarioId del JWT.
    /// 
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Frontend (Blazor): Consumido por Dashboard.razor y MisGastos.razor para listar/gestionar las suscripciones.
    /// - Backend (Service): Llama a SuscripcionService.cs, que a su vez se comunica con la BD mediante EF Core.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuscripcionesController : ControllerBase
    {
        private readonly ISuscripcionService _service;

        public SuscripcionesController(ISuscripcionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerMisSuscripciones()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var suscripciones = await _service.ObtenerPorUsuarioAsync(usuarioId.Value);
            return Ok(suscripciones);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] SuscripcionCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var resultado = await _service.CrearAsync(dto, usuarioId.Value);
            if (resultado == null)
                return BadRequest(new { mensaje = "No se pudo registrar la suscripción." });

            return CreatedAtAction(nameof(ObtenerMisSuscripciones), resultado);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var eliminado = await _service.EliminarAsync(id, usuarioId.Value);
            if (!eliminado)
                return NotFound(new { mensaje = "Suscripción no encontrada o no tienes permiso." });

            return NoContent();
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var actualizado = await _service.CambiarEstadoAsync(id, usuarioId.Value, request.Activa);
            if (!actualizado)
                return NotFound(new { mensaje = "Suscripción no encontrada." });

            return Ok(new { mensaje = "Estado actualizado." });
        }

        private int? ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out var id))
                return null;

            return id;
        }
    }

    public class CambiarEstadoRequest
    {
        public bool Activa { get; set; }
    }
}
