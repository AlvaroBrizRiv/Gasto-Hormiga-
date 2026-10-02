using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.DTOs
{
    // ════════════════════════════════════════════════════════════════════════════
    // DTÓS DEL CLASIFICADOR HÍBRIDO
    //
    // [CONEXIÓN ARQUITECTÓNICA]
    // - Origen (Backend): Generados por ClasificadorService.cs y devueltos por ClasificadorController.cs
    // - Destino (Frontend): Consumidos por Dashboard.razor.cs para mostrar sugerencias y alertas en la UI
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Respuesta del clasificador automático al consultar la sugerencia para un concepto.
    /// Contiene la categoría sugerida, si es hormiga, el nivel de confianza y de dónde viene la regla.
    /// </summary>
    public class SugerenciaClasificacionDto
    {
        /// <summary>Categoría sugerida por el clasificador</summary>
        public string CategoriasSugerida { get; set; } = string.Empty;

        /// <summary>¿Esta categoría cuenta como gasto hormiga por defecto?</summary>
        public bool EsHormigaPorDefecto { get; set; }

        /// <summary>
        /// Nivel de confianza del 0 al 100.
        /// 100 = regla de usuario (máxima confianza).
        /// 80 = keyword exacta encontrada en el diccionario.
        /// 40 = keyword parcial encontrada.
        /// 20 = fallback (sin regla específica).
        /// </summary>
        public int Confianza { get; set; }

        /// <summary>
        /// Origen de la regla que generó la sugerencia.
        /// "ReglaUsuario" | "DiccionarioComercio" | "Fallback"
        /// </summary>
        public string OrigenRegla { get; set; } = "Fallback";

        /// <summary>Si true, el comercio fue marcado como "Presupuestado" por el usuario → no preguntar de nuevo</summary>
        public bool EsPresupuestadoPorUsuario { get; set; }
    }

    /// <summary>
    /// Alerta de patrón de gasto para mostrar al usuario en el Dashboard.
    /// Se dispara cuando se detectan 3+ transacciones de la misma categoría en 7 días.
    /// </summary>
    public class AlertaPatronDto
    {
        /// <summary>ID de la alerta (necesario para resolverla vía PUT)</summary>
        public int Id { get; set; }

        /// <summary>Categoría que disparó el patrón detectado</summary>
        public string Categoria { get; set; } = string.Empty;

        /// <summary>Cantidad de transacciones detectadas en la ventana de 7 días</summary>
        public int CantidadTransacciones { get; set; }

        /// <summary>Monto total acumulado de las transacciones del patrón</summary>
        public decimal MontoAcumulado { get; set; }

        /// <summary>Inicio del período analizado (para mostrar en el mensaje)</summary>
        public DateTime PeriodoInicio { get; set; }

        /// <summary>Estado actual: "Pendiente" | "AceptadaHormiga" | "MarcadaPresupuestado"</summary>
        public string Estado { get; set; } = "Pendiente";
    }

    /// <summary>
    /// DTO que el frontend envía cuando el usuario decide marcar un comercio como "Presupuestado".
    /// Esto crea (o actualiza) una ReglasMercante en la BD para ese usuario.
    /// </summary>
    public class MarcarPresupuestadoDto
    {
        /// <summary>Texto del comercio o concepto a guardar como regla</summary>
        [Required]
        [MaxLength(200)]
        public string PalabraClave { get; set; } = string.Empty;

        /// <summary>
        /// Si true: se guarda como "Presupuestado" (no hormiga en el futuro).
        /// Si false: se guarda como "Hormiga" (siempre contar como fuga).
        /// </summary>
        public bool EsPresupuestado { get; set; }

        /// <summary>Categoría opcional que el usuario quiere forzar para este comercio</summary>
        [MaxLength(100)]
        public string? CategoriaOverride { get; set; }
    }

    /// <summary>
    /// DTO para resolver una alerta de patrón (cuando el usuario hace click en "Sí, es hormiga" o "No, es presupuesto").
    /// </summary>
    public class ResolverAlertaDto
    {
        /// <summary>"AceptadaHormiga" si el usuario confirma que es un gasto hormiga, "MarcadaPresupuestado" si no</summary>
        [Required]
        public string Decision { get; set; } = string.Empty;

        /// <summary>
        /// Si el usuario elige "No, es presupuesto", también se puede guardar la regla de mercante
        /// para que no vuelva a preguntar sobre este comercio en el futuro.
        /// </summary>
        public string? PalabraClaveComercio { get; set; }
    }
}
