using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GastoHormiga.Shared.Model
{
    /// <summary>
    /// Representa una suscripción o servicio de pago recurrente del usuario.
    /// (Netflix, Spotify, gimnasio, etc.)
    /// </summary>
    public class Suscripcion
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [MaxLength(200)]
        public string NombreServicio { get; private set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Costo { get; private set; }

        /// <summary>Día del mes en que se realiza el cobro (1-31)</summary>
        [Range(1, 31)]
        public int DiaPago { get; private set; }

        /// <summary>Periodicidad del cobro: "Mensual" o "Anual"</summary>
        [MaxLength(20)]
        public string Periodo { get; private set; } = "Mensual";

        public DateTime FechaProximoPago { get; private set; }
        public bool AlertaActiva { get; private set; }

        /// <summary>Indica si la suscripción está activa o fue cancelada</summary>
        public bool Activa { get; private set; } = true;

        /// <summary>
        /// El usuario decide si esta suscripción cuenta como gasto hormiga.
        /// true = afecta Fuga Mensual e Impacto Anual. false = solo registro informativo.
        /// </summary>
        public bool EsHormiga { get; private set; } = true;

        /// <summary>Fecha de registro</summary>
        public DateTime FechaRegistro { get; private set; } = DateTime.UtcNow;

        [ForeignKey("Usuario")]
        public int UsuarioId { get; private set; }

        [JsonIgnore]
        public Usuario? Usuario { get; private set; }

        /// <summary>Costo mensual equivalente (si es anual, divide por 12)</summary>
        [NotMapped]
        public decimal CostoMensualEquivalente =>
            Periodo.Equals("Anual", StringComparison.OrdinalIgnoreCase) ? Costo / 12 : Costo;

        /// <summary>Costo anual equivalente</summary>
        [NotMapped]
        public decimal CostoAnualEquivalente =>
            Periodo.Equals("Anual", StringComparison.OrdinalIgnoreCase) ? Costo : Costo * 12;

        /// <summary>
        /// Constructor de dominio con validaciones de negocio.
        /// </summary>
        public Suscripcion(string nombreServicio, decimal costo, int diaPago,
                           string periodo = "Mensual", DateTime? fechaProximoPago = null,
                           bool alertaActiva = false, int usuarioId = 0, bool esHormiga = true)
        {
            if (string.IsNullOrWhiteSpace(nombreServicio))
                throw new ArgumentException("El nombre del servicio es obligatorio.");
            if (costo <= 0)
                throw new ArgumentException("El costo debe ser mayor a cero.");
            if (diaPago < 1 || diaPago > 31)
                throw new ArgumentException("El día de pago debe estar entre 1 y 31.");

            NombreServicio = nombreServicio.Trim();
            Costo = costo;
            DiaPago = diaPago;
            Periodo = periodo;
            FechaProximoPago = fechaProximoPago ?? DateTime.UtcNow.AddMonths(1);
            AlertaActiva = alertaActiva;
            UsuarioId = usuarioId;
            EsHormiga = esHormiga;
            FechaRegistro = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancela/reactiva la suscripción.
        /// </summary>
        public void CambiarEstado(bool activa) => Activa = activa;

        /// <summary>
        /// Constructor vacío requerido por Entity Framework Core.
        /// </summary>
        [JsonConstructor]
        protected Suscripcion() { }
    }
}