using GastoHormiga.API.Data;
using GastoHormiga.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Servicio de gastos hormiga. Encapsula la lógica de CRUD y resumen financiero.
    /// Usa EsHormigaEfectivo para distinguir entre gastos hormiga y fijos al calcular métricas.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Dependencias: ApplicationDbContext (acceso a datos EF Core)
    /// - Consumido por: GastosHormigasController.cs (Capa API)
    /// - Intercambio de Datos: Usa DTOs (no entidades directas) hacia el frontend
    /// </summary>
    public class GastoHormigaService : IGastoHormigaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GastoHormigaService> _logger;

        public GastoHormigaService(ApplicationDbContext context, ILogger<GastoHormigaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GastoHormigaResponseDto>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            // Obtener todos los gastos del usuario, ordenados del más reciente al más antiguo
            var gastos = await _context.GastosHormiga
                .Where(g => g.UsuarioId == usuarioId)
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            // Mapear la entidad de dominio al DTO de respuesta
            return gastos.Select(g => MapToDto(g));
        }

        /// <inheritdoc/>
        public async Task<GastoHormigaResponseDto?> CrearAsync(GastoHormigaCreateDto dto, int usuarioId)
        {
            // Validar que el usuario existe en la BD (previene IDOR - OWASP A01)
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste)
            {
                _logger.LogWarning("Intento de crear gasto para usuario inexistente: {UsuarioId}", usuarioId);
                return null;
            }

            // Construir la entidad de dominio con todos sus datos
            var gasto = new Shared.Model.GastoHormiga(
                dto.Concepto,
                dto.CostoEstimado,
                dto.FrecuenciaSemanal,
                usuarioId,
                dto.Categoria,
                dto.Fecha,
                dto.TipoFrecuencia,
                dto.EsImportadoTarjeta,
                dto.OrigenTarjeta,
                dto.EsHormigaManual   // Override manual del usuario (puede ser null)
            );

            _context.GastosHormiga.Add(gasto);
            await _context.SaveChangesAsync();

            return MapToDto(gasto);
        }

        /// <inheritdoc/>
        public async Task<bool> EliminarAsync(int id, int usuarioId)
        {
            // Verificación de propiedad: solo el dueño puede eliminar su gasto (OWASP A01)
            var gasto = await _context.GastosHormiga
                .FirstOrDefaultAsync(g => g.Id == id && g.UsuarioId == usuarioId);

            if (gasto == null)
                return false;

            _context.GastosHormiga.Remove(gasto);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<ResumenGastosDto> ObtenerResumenAsync(int usuarioId, string divisaCodigo)
        {
            // Obtener todos los gastos del usuario
            var todosLosGastos = await _context.GastosHormiga
                .Where(g => g.UsuarioId == usuarioId)
                .ToListAsync();

            // Filtrar solo los gastos que SON efectivamente hormiga (respetando el override manual del usuario)
            // EsHormigaEfectivo combina la categoría por defecto con el override manual
            var gastosHormiga = todosLosGastos
                .Where(g => g.EsHormigaEfectivo)
                .ToList();

            // Proyección anual de los gastos hormiga únicamente
            var totalAnualHormiga = gastosHormiga.Sum(g => g.CalculoAnualizado);
            var decimales = string.Equals(divisaCodigo, "CLP", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
            decimal RedondearMonto(decimal monto) => Math.Round(monto, decimales, MidpointRounding.AwayFromZero);

            return new ResumenGastosDto
            {
                
                TotalSemanal = RedondearMonto(gastosHormiga.Sum(g => g.CalculoMensual / 4)),
                TotalMensual = RedondearMonto(gastosHormiga.Sum(g => g.CalculoMensual)),
                TotalAnual = RedondearMonto(totalAnualHormiga),
                AhorroPotencialMensual = RedondearMonto(totalAnualHormiga / 12),
                AhorroPotencialAnual = RedondearMonto(totalAnualHormiga),
                CantidadGastos = todosLosGastos.Count,   // Total de TODOS los registros (incluye fijos)
                DivisaCodigo = divisaCodigo
            };
        }

        /// <summary>
        /// Mapea una entidad de dominio GastoHormiga a su DTO de respuesta para el frontend.
        /// </summary>
        private static GastoHormigaResponseDto MapToDto(Shared.Model.GastoHormiga g) =>
            new GastoHormigaResponseDto
            {
                Id = g.Id,
                Concepto = g.Concepto,
                CostoEstimado = g.CostoEstimado,
                FrecuenciaSemanal = g.FrecuenciaSemanal,
                TipoFrecuencia = g.TipoFrecuencia,
                Categoria = g.Categoria,
                Fecha = g.Fecha,
                CalculoAnualizado = g.CalculoAnualizado,
                CalculoMensual = g.CalculoMensual,
                EsImportadoTarjeta = g.EsImportadoTarjeta,
                OrigenTarjeta = g.OrigenTarjeta,
                EsHormigaManual = g.EsHormigaManual      // Pasar el override al frontend
            };
    }
}
