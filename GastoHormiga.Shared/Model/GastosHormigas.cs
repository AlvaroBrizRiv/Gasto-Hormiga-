using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using GastoHormiga.Shared.DTOs;

namespace GastoHormiga.Shared.Model
{
    /// <summary>
    /// Representa un registro de gasto o pago del usuario.
    /// Puede ser un "gasto hormiga" (fuga pequeña recurrente) o un gasto fijo estructural.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Capa de Datos (EF Core): Mapeada a la tabla "GastosHormiga" en ApplicationDbContext.cs
    /// - Capa de Lógica: Instanciada y manipulada por GastoHormigaService.cs
    /// - Capa de Transporte: Mapeada hacia/desde GastoHormigaDtos.cs para comunicación con el frontend (Web)
    /// </summary>
    public class GastoHormiga
    {
        // ── PROPIEDADES DE IDENTIDAD ─────────────────────────────────────────────

        /// <summary>Identificador único del gasto (auto-generado por la BD)</summary>
        [Key]
        public int Id { get; private set; }

        // ── PROPIEDADES DE DATOS PRINCIPALES ────────────────────────────────────

        /// <summary>Descripción del gasto. Ej: "Café diario", "Rappi Delivery", "Netflix"</summary>
        [Required]
        [MaxLength(200)]
        public string Concepto { get; private set; } = string.Empty;

        /// <summary>Monto del gasto en la divisa del usuario</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEstimado { get; private set; }

        /// <summary>Número de veces por período (semanal o mensual). Ejemplo: 3 veces por semana</summary>
        [Required]
        public int FrecuenciaSemanal { get; private set; }

        /// <summary>Tipo de frecuencia: "Semanal", "Mensual" o "NoAplica" (pago único)</summary>
        [MaxLength(10)]
        public string TipoFrecuencia { get; private set; } = "Semanal";

        /// <summary>
        /// Categoría del gasto. Una de las 5 categorías definidas en CategoriasHormiga:
        /// "Alimentos & Antojos" | "Delivery & Conveniencia" | "Transporte & Movilidad" |
        /// "Suscripciones & Digital" | "Financiera"
        /// </summary>
        [MaxLength(100)]
        public string Categoria { get; private set; } = CategoriasHormiga.AlimentosAntojos;

        /// <summary>Fecha en que se registró el gasto (en UTC)</summary>
        public DateTime Fecha { get; private set; } = DateTime.UtcNow;

        // ── CLASIFICACIÓN INTELIGENTE (Human-in-the-Loop) ────────────────────────

        /// <summary>
        /// Override manual del usuario sobre si esta transacción específica es hormiga o no.
        /// null = usar el valor por defecto de la categoría (ver CategoriasHormiga.EsHormiga).
        /// true = el usuario lo marcó explícitamente como HORMIGA, aunque la categoría diga lo contrario.
        /// false = el usuario lo marcó como PRESUPUESTADO/NO-HORMIGA para esta transacción específica.
        ///
        /// Clave para "Transporte & Movilidad": el usuario puede decidir si un viaje particular
        /// es una fuga innecesaria o un gasto justificado.
        /// </summary>
        public bool? EsHormigaManual { get; private set; }

        // ── PROPIEDADES CALCULADAS (NO mapeadas a BD) ────────────────────────────

        /// <summary>
        /// Determina si esta transacción EFECTIVAMENTE cuenta como gasto hormiga.
        /// Combina el override del usuario con el valor por defecto de la categoría.
        /// </summary>
        [NotMapped]
        public bool EsHormigaEfectivo =>
            EsHormigaManual.HasValue
                ? EsHormigaManual.Value                          // Override explícito del usuario
                : CategoriasHormiga.EsHormiga(Categoria);        // Valor por defecto de la categoría

        /// <summary>
        /// Proyección del costo al año completo basado en la frecuencia.
        /// "NoAplica" → el gasto se registra como valor absoluto sin anualizarlo.
        /// </summary>
        [NotMapped]
        public decimal CalculoAnualizado =>
            TipoFrecuencia == "NoAplica" ? CostoEstimado :
            TipoFrecuencia == "Mensual"
                ? CostoEstimado * FrecuenciaSemanal * 12
                : CostoEstimado * FrecuenciaSemanal * 52;

        /// <summary>
        /// Proyección del costo mensual estimado basado en la frecuencia.
        /// "NoAplica" → trata el monto como si fuera mensual para el resumen.
        /// </summary>
        [NotMapped]
        public decimal CalculoMensual =>
            TipoFrecuencia == "NoAplica" ? CostoEstimado :
            TipoFrecuencia == "Mensual"
                ? CostoEstimado * FrecuenciaSemanal
                : CostoEstimado * FrecuenciaSemanal * 52m / 12m;

        // ── IMPORTACIÓN BANCARIA ─────────────────────────────────────────────────

        /// <summary>Indica si el gasto fue importado desde un movimiento de tarjeta (simulación bancaria)</summary>
        public bool EsImportadoTarjeta { get; private set; } = false;

        /// <summary>Origen del movimiento bancario: "TarjetaDebito", "TarjetaCredito" o null si es manual</summary>
        [MaxLength(20)]
        public string? OrigenTarjeta { get; private set; }

        // ── RELACIÓN CON USUARIO ─────────────────────────────────────────────────

        /// <summary>ID del usuario propietario del gasto (FK a la tabla Usuarios)</summary>
        [ForeignKey("Usuario")]
        public int UsuarioId { get; private set; }

        /// <summary>Relación de navegación con el Usuario (solo backend, ignorada en serialización JSON)</summary>
        [JsonIgnore]
        public Usuario? Usuario { get; private set; }

        // ── CONSTRUCTORES ────────────────────────────────────────────────────────

        /// <summary>
        /// Constructor de dominio con validaciones de negocio.
        /// Garantiza que no se puedan crear gastos inválidos.
        /// </summary>
        public GastoHormiga(string concepto, decimal costoEstimado, int frecuencia,
                             int usuarioId, string categoria,
                             DateTime? fecha = null, string tipoFrecuencia = "Semanal",
                             bool esImportadoTarjeta = false, string? origenTarjeta = null,
                             bool? esHormigaManual = null)
        {
            if (string.IsNullOrWhiteSpace(concepto))
                throw new ArgumentException("El concepto es obligatorio.");
            if (costoEstimado <= 0)
                throw new ArgumentException("El costo estimado debe ser mayor a cero.");
            if (frecuencia <= 0 && tipoFrecuencia != "NoAplica")
                throw new ArgumentException("La frecuencia debe ser mayor a cero.");
            if (usuarioId <= 0)
                throw new ArgumentException("El UsuarioId es inválido.");

            Concepto = concepto.Trim();
            CostoEstimado = costoEstimado;
            // Si la frecuencia no aplica, internamente se guarda como 1 para evitar multiplicar por 0
            FrecuenciaSemanal = tipoFrecuencia == "NoAplica" ? 1 : frecuencia;
            UsuarioId = usuarioId;
            Categoria = string.IsNullOrWhiteSpace(categoria) ? CategoriasHormiga.AlimentosAntojos : categoria.Trim();
            Fecha = fecha ?? DateTime.UtcNow;
            TipoFrecuencia = tipoFrecuencia == "Mensual" ? "Mensual" :
                             tipoFrecuencia == "NoAplica" ? "NoAplica" : "Semanal";
            EsImportadoTarjeta = esImportadoTarjeta;
            OrigenTarjeta = origenTarjeta;
            EsHormigaManual = esHormigaManual;
        }

        /// <summary>Constructor vacío requerido por Entity Framework Core</summary>
        [JsonConstructor]
        protected GastoHormiga() { }
    }
}
