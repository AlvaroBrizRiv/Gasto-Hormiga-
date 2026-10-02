using System;
using System.ComponentModel.DataAnnotations;

namespace GastoHormiga.Shared.DTOs
{
    /// <summary>
    /// DTO para crear o actualizar una suscripción recurrente.
    /// EsHormiga permite que el usuario clasifique si la suscripción es una fuga hormiga.
    /// </summary>
    public class SuscripcionCreateDto
    {
        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [MaxLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
        public string NombreServicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El costo es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a cero.")]
        public decimal Costo { get; set; }

        [Range(1, 31, ErrorMessage = "El día de pago debe estar entre 1 y 31.")]
        public int DiaPago { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        public string Periodo { get; set; } = "Mensual";

        public DateTime? FechaProximoPago { get; set; }
        public bool AlertaActiva { get; set; } = false;

        /// <summary>Si es true, se contabiliza en Fuga Mensual e Impacto Anual. El usuario lo decide al registrar.</summary>
        public bool EsHormiga { get; set; } = true;
    }

    /// <summary>
    /// DTO de respuesta con datos de una suscripción.
    /// </summary>
    public class SuscripcionResponseDto
    {
        public int Id { get; set; }
        public string NombreServicio { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public int DiaPago { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public DateTime FechaProximoPago { get; set; }
        public bool AlertaActiva { get; set; }
        public bool Activa { get; set; }
        public decimal CostoMensualEquivalente { get; set; }
        public decimal CostoAnualEquivalente { get; set; }
        public int DiasParaProximoCobro { get; set; }

        /// <summary>Indica si esta suscripción cuenta como fuga hormiga. Afecta Fuga Mensual e Impacto Anual.</summary>
        public bool EsHormiga { get; set; } = true;
    }
}

