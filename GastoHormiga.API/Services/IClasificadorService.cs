using GastoHormiga.Shared.DTOs;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Contrato del motor de clasificación híbrido.
    /// Define las 3 operaciones del sistema Human-in-the-Loop.
    /// Principio: Interface Segregation (SOLID-I).
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Implementado por: ClasificadorService.cs
    /// - Consumido por: ClasificadorController.cs
    /// </summary>
    public interface IClasificadorService
    {
        /// <summary>
        /// Capa 1: Busca si el usuario ya tiene una regla para este comercio.
        /// Capa 2: Si no hay regla, busca keywords en el diccionario de comercios.
        /// Capa 3: Fallback con baja confianza si no hay ninguna regla aplicable.
        /// </summary>
        Task<SugerenciaClasificacionDto> SugerirClasificacionAsync(string concepto, int usuarioId);

        /// <summary>
        /// Analiza los gastos recientes del usuario y retorna alertas de patrones pendientes.
        /// Un patrón se dispara cuando hay 3+ transacciones de la misma categoría en 7 días.
        /// </summary>
        Task<IEnumerable<AlertaPatronDto>> ObtenerAlertasPendientesAsync(int usuarioId);

        /// <summary>
        /// Guarda la decisión del usuario sobre cómo clasificar un comercio/concepto en el futuro.
        /// Crea o actualiza la ReglaMercante correspondiente.
        /// </summary>
        Task MarcarComercioAsync(MarcarPresupuestadoDto dto, int usuarioId);

        /// <summary>
        /// Resuelve una alerta de patrón con la decisión del usuario.
        /// Si eligió "MarcadaPresupuestado", también crea la ReglasMercante para no volver a preguntar.
        /// </summary>
        Task<bool> ResolverAlertaAsync(int alertaId, ResolverAlertaDto dto, int usuarioId);

        /// <summary>
        /// Ejecuta la detección de patrones para un usuario y crea nuevas alertas si corresponde.
        /// Se llama automáticamente después de registrar un nuevo gasto.
        /// </summary>
        Task EjecutarDeteccionDePatronesAsync(int usuarioId);
    }
}
