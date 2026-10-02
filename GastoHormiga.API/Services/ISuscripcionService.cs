using GastoHormiga.Shared.DTOs;

namespace GastoHormiga.API.Services
{
    public interface ISuscripcionService
    {
        Task<IEnumerable<SuscripcionResponseDto>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<SuscripcionResponseDto?> CrearAsync(SuscripcionCreateDto dto, int usuarioId);
        Task<bool> EliminarAsync(int id, int usuarioId);
        Task<bool> CambiarEstadoAsync(int id, int usuarioId, bool activa);
    }
}
