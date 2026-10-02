using GastoHormiga.API.Data;
using GastoHormiga.Shared.DTOs;
using GastoHormiga.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Servicio de suscripciones. Verifica propiedad antes de cualquier operación (OWASP A01).
    /// </summary>
    public class SuscripcionService : ISuscripcionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuscripcionService> _logger;

        public SuscripcionService(ApplicationDbContext context, ILogger<SuscripcionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SuscripcionResponseDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            var suscripciones = await _context.Suscripciones
                .Where(s => s.UsuarioId == usuarioId)
                .OrderBy(s => s.FechaProximoPago)
                .ToListAsync();

            return suscripciones.Select(MapToDto);
        }

        public async Task<SuscripcionResponseDto?> CrearAsync(SuscripcionCreateDto dto, int usuarioId)
        {
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste) return null;

            var suscripcion = new Suscripcion(
                dto.NombreServicio,
                dto.Costo,
                dto.DiaPago,
                dto.Periodo,
                dto.FechaProximoPago,
                dto.AlertaActiva,
                usuarioId,
                dto.EsHormiga          // El usuario indicó si es fuga hormiga o gasto fijo
            );

            _context.Suscripciones.Add(suscripcion);
            await _context.SaveChangesAsync();

            return MapToDto(suscripcion);
        }

        public async Task<bool> EliminarAsync(int id, int usuarioId)
        {
            // Verificación de propiedad (OWASP A01 - IDOR prevention)
            var suscripcion = await _context.Suscripciones
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

            if (suscripcion == null) return false;

            _context.Suscripciones.Remove(suscripcion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CambiarEstadoAsync(int id, int usuarioId, bool activa)
        {
            var suscripcion = await _context.Suscripciones
                .FirstOrDefaultAsync(s => s.Id == id && s.UsuarioId == usuarioId);

            if (suscripcion == null) return false;

            suscripcion.CambiarEstado(activa);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SuscripcionResponseDto MapToDto(Suscripcion s) =>
            new SuscripcionResponseDto
            {
                Id = s.Id,
                NombreServicio = s.NombreServicio,
                Costo = s.Costo,
                DiaPago = s.DiaPago,
                Periodo = s.Periodo,
                FechaProximoPago = s.FechaProximoPago,
                AlertaActiva = s.AlertaActiva,
                Activa = s.Activa,
                EsHormiga = s.EsHormiga,
                CostoMensualEquivalente = s.CostoMensualEquivalente,
                CostoAnualEquivalente = s.CostoAnualEquivalente,
                DiasParaProximoCobro = (s.FechaProximoPago.Date - DateTime.UtcNow.Date).Days
            };
    }
}
