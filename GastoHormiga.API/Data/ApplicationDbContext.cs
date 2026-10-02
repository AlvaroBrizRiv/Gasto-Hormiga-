using Microsoft.EntityFrameworkCore;
using GastoHormiga.Shared.Model;

namespace GastoHormiga.API.Data
{
    /// <summary>
    /// Contexto de base de datos de Entity Framework Core.
    /// Configura todas las entidades, relaciones e índices del sistema.
    /// Principio: Single Responsibility — solo gestiona el acceso a datos.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Registrado en Program.cs (API) para inyección de dependencias (conectado a SQL Server).
    /// - Inyectado en Servicios (GastoHormigaService, AuthService, ClasificadorService) para CRUD.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // ── TABLAS PRINCIPALES ───────────────────────────────────────────────────

        /// <summary>Tabla de usuarios registrados en el sistema</summary>
        public DbSet<Usuario> Usuarios { get; set; }

        /// <summary>Tabla de suscripciones digitales con alertas de cobro</summary>
        public DbSet<Suscripcion> Suscripciones { get; set; }

        /// <summary>Tabla de todos los gastos registrados (hormiga y fijos)</summary>
        public DbSet<GastoHormiga.Shared.Model.GastoHormiga> GastosHormiga { get; set; }

        // ── TABLAS DEL MOTOR DE CLASIFICACIÓN HÍBRIDO ───────────────────────────

        /// <summary>
        /// Reglas personalizadas de mercante por usuario.
        /// Cada fila representa un comercio que el usuario marcó como "Presupuestado" o "Hormiga".
        /// </summary>
        public DbSet<ReglaMercante> ReglasMercante { get; set; }

        /// <summary>
        /// Alertas generadas cuando el clasificador detecta 3+ transacciones similares en 7 días.
        /// El usuario las resuelve desde el Dashboard.
        /// </summary>
        public DbSet<AlertaPatron> AlertasPatron { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── USUARIO ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Telefono).IsRequired().HasMaxLength(20);
                entity.Property(u => u.CodigoPais).IsRequired().HasMaxLength(5);
                entity.Property(u => u.DivisaCodigo).IsRequired().HasMaxLength(10);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.CodigoOtp).HasMaxLength(6);

                // Índices únicos para prevenir registros duplicados (OWASP A04)
                entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Usuarios_Email");
                entity.HasIndex(u => u.Telefono).IsUnique().HasDatabaseName("IX_Usuarios_Telefono");
            });

            // ── SUSCRIPCIÓN ───────────────────────────────────────────────────────
            modelBuilder.Entity<Suscripcion>(entity =>
            {
                entity.ToTable("Suscripciones");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.NombreServicio).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Costo).HasColumnType("decimal(18,2)");
                entity.Property(s => s.Periodo).HasMaxLength(20).HasDefaultValue("Mensual");

                entity.HasOne(s => s.Usuario)
                    .WithMany(u => u.Suscripciones)
                    .HasForeignKey(s => s.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── GASTO HORMIGA ─────────────────────────────────────────────────────
            modelBuilder.Entity<GastoHormiga.Shared.Model.GastoHormiga>(entity =>
            {
                entity.ToTable("GastosHormiga");
                entity.HasKey(g => g.Id);

                entity.Property(g => g.Concepto).IsRequired().HasMaxLength(200);
                entity.Property(g => g.CostoEstimado).HasColumnType("decimal(18,2)");
                entity.Property(g => g.Categoria).HasMaxLength(100).HasDefaultValue("Alimentos & Antojos");
                entity.Property(g => g.Fecha).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(g => g.TipoFrecuencia).HasMaxLength(10).HasDefaultValue("Semanal");
                entity.Property(g => g.EsImportadoTarjeta).HasDefaultValue(false);
                entity.Property(g => g.OrigenTarjeta).HasMaxLength(20).IsRequired(false);
                // EsHormigaManual: nullable bool, null = usar default de la categoría
                entity.Property(g => g.EsHormigaManual).IsRequired(false);

                entity.HasOne(g => g.Usuario)
                    .WithMany()
                    .HasForeignKey(g => g.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice compuesto para consultas frecuentes por usuario y fecha
                entity.HasIndex(g => new { g.UsuarioId, g.Fecha })
                    .HasDatabaseName("IX_GastosHormiga_UsuarioId_Fecha");
            });

            // ── REGLAS MERCANTE (Motor Híbrido - Capa 1) ─────────────────────────
            modelBuilder.Entity<ReglaMercante>(entity =>
            {
                entity.ToTable("ReglasMercante");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.PalabraClave).IsRequired().HasMaxLength(200);
                entity.Property(r => r.CategoriaOverride).HasMaxLength(100).IsRequired(false);
                entity.Property(r => r.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(r => r.Usuario)
                    .WithMany(u => u.ReglasMercante)
                    .HasForeignKey(r => r.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice único: un usuario no puede tener dos reglas para la misma palabra clave
                entity.HasIndex(r => new { r.UsuarioId, r.PalabraClave })
                    .IsUnique()
                    .HasDatabaseName("IX_ReglasMercante_UsuarioId_PalabraClave");
            });

            // ── ALERTAS DE PATRÓN (Motor Híbrido - Capa 3) ───────────────────────
            modelBuilder.Entity<AlertaPatron>(entity =>
            {
                entity.ToTable("AlertasPatron");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Categoria).IsRequired().HasMaxLength(100);
                entity.Property(a => a.MontoAcumulado).HasColumnType("decimal(18,2)");
                entity.Property(a => a.Estado).HasMaxLength(30).HasDefaultValue("Pendiente");
                entity.Property(a => a.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(a => a.FechaResolucion).IsRequired(false);

                entity.HasOne(a => a.Usuario)
                    .WithMany(u => u.AlertasPatron)
                    .HasForeignKey(a => a.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice para consultas frecuentes de alertas pendientes por usuario
                entity.HasIndex(a => new { a.UsuarioId, a.Estado })
                    .HasDatabaseName("IX_AlertasPatron_UsuarioId_Estado");
            });
        }
    }
}
