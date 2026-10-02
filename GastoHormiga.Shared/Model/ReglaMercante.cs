using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GastoHormiga.Shared.Model
{
    /// <summary>
    /// Regla personalizada de un usuario sobre un comercio o concepto.
    /// Permite que el usuario indique si cierto comercio es siempre "presupuestado" (no hormiga)
    /// o siempre "hormiga", evitando que la app le vuelva a preguntar.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Capa de Datos (EF Core): Mapeada a la tabla "ReglasMercante" en ApplicationDbContext.cs
    /// - Capa de Lógica: Leída por ClasificadorService.cs como Capa 1 (regla de usuario = máxima prioridad)
    /// - Capa de Transporte: Creada desde el frontend cuando el usuario hace clic en "Marcar como presupuestado"
    /// </summary>
    public class ReglaMercante
    {
        /// <summary>Identificador único de la regla</summary>
        [Key]
        public int Id { get; private set; }

        /// <summary>ID del usuario propietario de la regla (FK a Usuarios)</summary>
        public int UsuarioId { get; private set; }

        /// <summary>
        /// Texto clave del comercio o concepto.
        /// El clasificador hace comparación case-insensitive y por "contiene" (Contains).
        /// Ejemplo: "Rappi", "McDonald", "Starbucks"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string PalabraClave { get; private set; } = string.Empty;

        /// <summary>
        /// Si true: el usuario marcó este comercio como "Presupuestado" (no es hormiga aunque la categoría lo diga).
        /// Si false: el usuario lo marcó explícitamente como "Hormiga" (siempre cuenta como fuga).
        /// </summary>
        public bool EsPresupuestado { get; private set; }

        /// <summary>
        /// Categoría específica que el usuario quiere asignar a este comercio.
        /// Si es null, se usa la categoría por defecto del clasificador.
        /// </summary>
        [MaxLength(100)]
        public string? CategoriaOverride { get; private set; }

        /// <summary>Fecha de creación de la regla</summary>
        public DateTime FechaCreacion { get; private set; } = DateTime.UtcNow;

        /// <summary>Relación de navegación con el usuario (solo backend, ignorada en serialización JSON)</summary>
        [JsonIgnore]
        public Usuario? Usuario { get; private set; }

        /// <summary>Constructor de dominio para crear una nueva regla de mercante</summary>
        public ReglaMercante(int usuarioId, string palabraClave, bool esPresupuestado, string? categoriaOverride = null)
        {
            if (usuarioId <= 0) throw new ArgumentException("UsuarioId inválido.");
            if (string.IsNullOrWhiteSpace(palabraClave)) throw new ArgumentException("La palabra clave es obligatoria.");

            UsuarioId = usuarioId;
            PalabraClave = palabraClave.Trim().ToLowerInvariant();
            EsPresupuestado = esPresupuestado;
            CategoriaOverride = categoriaOverride;
            FechaCreacion = DateTime.UtcNow;
        }

        /// <summary>Constructor vacío requerido por Entity Framework Core</summary>
        [JsonConstructor]
        protected ReglaMercante() { }
    }
}
