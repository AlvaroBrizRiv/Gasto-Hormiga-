using GastoHormiga.Shared.DTOs;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Contrato del servicio de gastos hormiga.
    /// Principio: Interface Segregation (SOLID-I).
    /// </summary>
    public interface IGastoHormigaService
    {
        Task<IEnumerable<GastoHormigaResponseDto>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<GastoHormigaResponseDto?> CrearAsync(GastoHormigaCreateDto dto, int usuarioId);
        Task<bool> EliminarAsync(int id, int usuarioId);
        Task<ResumenGastosDto> ObtenerResumenAsync(int usuarioId, string divisaCodigo);
    }
}
