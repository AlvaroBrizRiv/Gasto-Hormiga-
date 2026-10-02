using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GastoHormiga.Shared.Model
{
    /// <summary>
    /// Alerta de patrón detectado por el motor de aprendizaje.
    /// Se crea cuando el backend detecta 3+ transacciones de la misma categoría
    /// en un período de 7 días, para preguntarle al usuario si quiere monitorearlo.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Capa de Datos (EF Core): Mapeada a la tabla "AlertasPatron" en ApplicationDbContext.cs
    /// - Capa de Lógica: Creada por ClasificadorService.DetectarPatrones() y leída por ClasificadorController.
    /// - Capa de Transporte: Enviada al frontend (AlertaPatronDto) para mostrarse en el Dashboard como banner.
    /// </summary>
    public class AlertaPatron
    {
        /// <summary>Identificador único de la alerta</summary>
        [Key]
        public int Id { get; private set; }

        /// <summary>ID del usuario al que pertenece la alerta</summary>
        public int UsuarioId { get; private set; }

        /// <summary>
        /// Categoría que disparó el patrón.
        /// Ej: "Delivery & Conveniencia", "Alimentos & Antojos"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Categoria { get; private set; } = string.Empty;

        /// <summary>Número de transacciones detectadas en el período</summary>
        public int CantidadTransacciones { get; private set; }

        /// <summary>Monto total acumulado de las transacciones del patrón</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoAcumulado { get; private set; }

        /// <summary>Período de inicio del análisis (inicio de la ventana de 7 días)</summary>
        public DateTime PeriodoInicio { get; private set; }

        /// <summary>Estado de la alerta: "Pendiente", "AceptadaHormiga", "MarcadaPresupuestado"</summary>
        [MaxLength(30)]
        public string Estado { get; private set; } = "Pendiente";

        /// <summary>Fecha en que se creó la alerta</summary>
        public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

        /// <summary>Fecha en que el usuario respondió a la alerta (null si aún es Pendiente)</summary>
        public DateTime? FechaResolucion { get; private set; }

        /// <summary>Relación de navegación con el usuario</summary>
        [JsonIgnore]
        public Usuario? Usuario { get; private set; }

        /// <summary>Constructor de dominio para crear una nueva alerta de patrón</summary>
        public AlertaPatron(int usuarioId, string categoria, int cantidad, decimal monto, DateTime periodoInicio)
        {
            if (usuarioId <= 0) throw new ArgumentException("UsuarioId inválido.");
            if (string.IsNullOrWhiteSpace(categoria)) throw new ArgumentException("La categoría es obligatoria.");
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");

            UsuarioId = usuarioId;
            Categoria = categoria.Trim();
            CantidadTransacciones = cantidad;
            MontoAcumulado = monto;
            PeriodoInicio = periodoInicio;
            Estado = "Pendiente";
            FechaCreacion = DateTime.UtcNow;
        }

        /// <summary>
        /// Resuelve la alerta cuando el usuario toma una decisión.
        /// </summary>
        /// <param name="estado">"AceptadaHormiga" o "MarcadaPresupuestado"</param>
        public void Resolver(string estado)
        {
            Estado = estado;
            FechaResolucion = DateTime.UtcNow;
        }

        /// <summary>Constructor vacío requerido por Entity Framework Core</summary>
        [JsonConstructor]
        protected AlertaPatron() { }
    }
}
