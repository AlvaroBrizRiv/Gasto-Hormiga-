using System;
using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.DTOs
{
    /// <summary>
    /// Definición centralizada de las 5 categorías del sistema y su clasificación como "hormiga".
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Utilizada por GastoHormigaService.cs (API) para filtrar métricas antes de enviar al frontend.
    /// - Utilizada por ClasificadorService.cs (API) como referencia de clasificación por defecto.
    /// - Utilizada por Dashboard.razor.cs y MisGastos.razor.cs (Web) para aplicar colores/badges a la UI.
    ///
    /// REGLA DE NEGOCIO:
    /// - "Transporte & Movilidad" es la única categoría ambigua: el usuario decide per-transacción mediante el flag EsHormigaManual.
    /// - Todas las demás tienen una clasificación por defecto fija.
    /// </summary>
    public static class CategoriasHormiga
    {
        // ── CONSTANTES DE NOMBRE DE CATEGORÍA ──────────────────────────────────
        // Usar estas constantes en lugar de strings literales para evitar errores tipográficos

        public const string AlimentosAntojos = "Alimentos & Antojos";
        public const string DeliveryConveniencia = "Delivery & Conveniencia";
        public const string TransporteMovilidad = "Transporte & Movilidad";
        public const string SuscripcionesDigital = "Suscripciones & Digital";
        public const string Financiera = "Financiera";

        // ── CATEGORÍAS QUE SON HORMIGA POR DEFECTO ──────────────────────────────
        // Todas las categorías excepto "Financiera" son hormiga por defecto.
        // El usuario puede cambiar esto en cada transacción con el toggle de la UI.

        public static readonly HashSet<string> Hormiga = new(StringComparer.OrdinalIgnoreCase)
        {
            AlimentosAntojos,
            DeliveryConveniencia,
            TransporteMovilidad,     // Ahora también hormiga por defecto (usuario decide)
            SuscripcionesDigital
        };

        /// <summary>
        /// Determina si una categoría es "hormiga" por defecto.
        /// Para "Transporte & Movilidad", usar el flag EsHormigaManual de la transacción.
        /// </summary>
        public static bool EsHormiga(string categoria) => Hormiga.Contains(categoria);

        // ── MAPA DE MIGRACIÓN DE CATEGORÍAS ANTIGUAS → NUEVAS ──────────────────
        // Usado en la migración de datos de EF Core para actualizar registros históricos

        public static readonly Dictionary<string, string> MapaMigracion = new(StringComparer.OrdinalIgnoreCase)
        {
            { "General",            AlimentosAntojos     },
            { "Otros",              AlimentosAntojos     },
            { "Entretenimiento",    SuscripcionesDigital },
            { "Suscripciones",      SuscripcionesDigital },
            { "Alimentacion",       AlimentosAntojos     },
            { "Transporte",         TransporteMovilidad  },
            { "Salud",              Financiera           },
            { "Arriendo/Dividendo", Financiera           },
            { "PagoCuotaAuto",      Financiera           },
            { "PagoPrestamos",      Financiera           },
            { "TarjetaCredito",     Financiera           },
        };

        /// <summary>Todas las categorías disponibles en el sistema, en orden de presentación en la UI</summary>
        public static readonly string[] Todas = new[]
        {
            AlimentosAntojos,
            DeliveryConveniencia,
            TransporteMovilidad,
            SuscripcionesDigital,
            Financiera
        };
    }

    /// <summary>
    /// DTO para crear un nuevo gasto hormiga.
    /// El UsuarioId se obtiene del JWT en el servidor (no se envía desde el cliente).
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Origen (Frontend): Creado en Dashboard.razor.cs y enviado vía POST (HttpClient).
    /// - Destino (Backend): Recibido por GastosHormigasController.cs, validado, y pasado a GastoHormigaService.cs
    /// </summary>
    public class GastoHormigaCreateDto
    {
        [Required(ErrorMessage = "El concepto es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El concepto no puede superar 200 caracteres.")]
        public string Concepto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El costo estimado es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a cero.")]
        public decimal CostoEstimado { get; set; }

        /// <summary>
        /// Número de veces por período. Ignorado si TipoFrecuencia es "NoAplica".
        /// Validación condicional: no se valida si TipoFrecuencia == "NoAplica"
        /// </summary>
        public int FrecuenciaSemanal { get; set; } = 1;

        /// <summary>"Semanal" | "Mensual" | "NoAplica"</summary>
        public string TipoFrecuencia { get; set; } = "Semanal";

        /// <summary>Una de las 5 categorías definidas en CategoriasHormiga</summary>
        [MaxLength(100)]
        public string Categoria { get; set; } = CategoriasHormiga.AlimentosAntojos;

        public DateTime? Fecha { get; set; }

        /// <summary>
        /// Override manual del usuario sobre si esta transacción específica es hormiga o no.
        /// null = usar el valor por defecto de la categoría.
        /// true = el usuario lo marcó explícitamente como hormiga.
        /// false = el usuario lo marcó explícitamente como no-hormiga (presupuestado).
        /// Clave para la categoría ambigua "Transporte & Movilidad".
        /// </summary>
        public bool? EsHormigaManual { get; set; }

        /// <summary>true si el gasto viene de un movimiento bancario simulado (importar tarjeta)</summary>
        public bool EsImportadoTarjeta { get; set; } = false;

        /// <summary>"TarjetaDebito" | "TarjetaCredito" | null</summary>
        public string? OrigenTarjeta { get; set; }
    }

    /// <summary>
    /// DTO de respuesta con datos de un gasto hormiga.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Origen (Backend): Generado en GastoHormigaService.cs y retornado por GastosHormigasController.cs
    /// - Destino (Frontend): Consumido por Dashboard.razor.cs y MisGastos.razor.cs para listar gastos
    /// </summary>
    public class GastoHormigaResponseDto
    {
        public int Id { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal CostoEstimado { get; set; }
        public int FrecuenciaSemanal { get; set; }
        public string TipoFrecuencia { get; set; } = "Semanal";
        public string Categoria { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal CalculoAnualizado { get; set; }
        public decimal CalculoMensual { get; set; }
        public bool EsImportadoTarjeta { get; set; }
        public string? OrigenTarjeta { get; set; }

        /// <summary>Override del usuario: null = usa default de la categoría, true/false = explícito</summary>
        public bool? EsHormigaManual { get; set; }

        /// <summary>
        /// Propiedad calculada: indica si esta transacción EFECTIVAMENTE es hormiga,
        /// considerando tanto la categoría por defecto como el override manual del usuario.
        /// </summary>
        public bool EsHormigaEfectivo =>
            EsHormigaManual.HasValue
                ? EsHormigaManual.Value                         // El usuario decidió explícitamente
                : CategoriasHormiga.EsHormiga(Categoria);       // Usar el valor por defecto de la categoría
    }

    /// <summary>
    /// DTO con el resumen financiero de gastos hormiga por período.
    /// Solo incluye transacciones donde EsHormigaEfectivo = true.
    /// </summary>
    public class ResumenGastosDto
    {
        public decimal TotalSemanal { get; set; }
        public decimal TotalMensual { get; set; }
        public decimal TotalAnual { get; set; }
        public decimal AhorroPotencialMensual { get; set; }
        public decimal AhorroPotencialAnual { get; set; }
        public int CantidadGastos { get; set; }
        public string DivisaCodigo { get; set; } = "CLP";
    }
}
