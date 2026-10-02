using GastoHormiga.API.Data;
using GastoHormiga.Shared.DTOs;
using GastoHormiga.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace GastoHormiga.API.Services
{
    /// <summary>
    /// Motor de clasificación híbrido con aprendizaje por retroalimentación (Human-in-the-Loop).
    /// Opera en 3 capas de prioridad decreciente:
    ///   1. Regla de Usuario (ReglaMercante en BD) → Máxima prioridad, siempre se respeta
    ///   2. Diccionario de Comercios (keywords estáticas) → Alta confianza si hay match
    ///   3. Fallback → Baja confianza, invita al usuario a confirmar
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Implementa: IClasificadorService.cs
    /// - Dependencias: ApplicationDbContext (para ReglasMercante y AlertasPatron)
    /// - Consumido por: ClasificadorController.cs (vía inyección de dependencias)
    /// - Llamado también desde: GastosHormigasController.cs (post-guardado para detección de patrones)
    /// </summary>
    public class ClasificadorService : IClasificadorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClasificadorService> _logger;

        // ── CAPA 2: DICCIONARIO DE COMERCIOS ─────────────────────────────────────
        // Mapa de palabras clave (insensible a mayúsculas) → categoría sugerida.
        // Las keywords se evalúan con Contains() para capturar variaciones del nombre.
        private static readonly Dictionary<string, string> _diccionarioComercios =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // 🍔 Alimentos & Antojos
            { "mcdonald",     CategoriasHormiga.AlimentosAntojos },
            { "burger king",  CategoriasHormiga.AlimentosAntojos },
            { "kfc",          CategoriasHormiga.AlimentosAntojos },
            { "wendy",        CategoriasHormiga.AlimentosAntojos },
            { "starbucks",    CategoriasHormiga.AlimentosAntojos },
            { "cafe",         CategoriasHormiga.AlimentosAntojos },
            { "café",         CategoriasHormiga.AlimentosAntojos },
            { "panaderia",    CategoriasHormiga.AlimentosAntojos },
            { "pasteleria",   CategoriasHormiga.AlimentosAntojos },
            { "snack",        CategoriasHormiga.AlimentosAntojos },
            { "heladeria",    CategoriasHormiga.AlimentosAntojos },
            { "frugos",       CategoriasHormiga.AlimentosAntojos },
            { "jugo",         CategoriasHormiga.AlimentosAntojos },
            { "minimarket",   CategoriasHormiga.AlimentosAntojos },
            { "supermercado", CategoriasHormiga.AlimentosAntojos },
            { "lider",        CategoriasHormiga.AlimentosAntojos },
            { "jumbo",        CategoriasHormiga.AlimentosAntojos },
            { "unimarc",      CategoriasHormiga.AlimentosAntojos },
            { "tottus",       CategoriasHormiga.AlimentosAntojos },
            { "sushi",        CategoriasHormiga.AlimentosAntojos },
            { "pizza",        CategoriasHormiga.AlimentosAntojos },
            { "dominos",      CategoriasHormiga.AlimentosAntojos },
            { "papa johns",   CategoriasHormiga.AlimentosAntojos },
            { "comida",       CategoriasHormiga.AlimentosAntojos },

            // 🛵 Delivery & Conveniencia
            { "rappi",        CategoriasHormiga.DeliveryConveniencia },
            { "uber eats",    CategoriasHormiga.DeliveryConveniencia },
            { "pedidosya",    CategoriasHormiga.DeliveryConveniencia },
            { "justo",        CategoriasHormiga.DeliveryConveniencia },
            { "cornershop",   CategoriasHormiga.DeliveryConveniencia },
            { "delivery",     CategoriasHormiga.DeliveryConveniencia },
            { "amazon",       CategoriasHormiga.DeliveryConveniencia },
            { "aliexpress",   CategoriasHormiga.DeliveryConveniencia },
            { "mercadolibre", CategoriasHormiga.DeliveryConveniencia },
            { "falabella",    CategoriasHormiga.DeliveryConveniencia },
            { "ripley",       CategoriasHormiga.DeliveryConveniencia },
            { "paris",        CategoriasHormiga.DeliveryConveniencia },
            { "shein",        CategoriasHormiga.DeliveryConveniencia },

            // 🚌 Transporte & Movilidad (ambiguo — el usuario decide)
            { "uber",         CategoriasHormiga.TransporteMovilidad },
            { "cabify",       CategoriasHormiga.TransporteMovilidad },
            { "indriver",     CategoriasHormiga.TransporteMovilidad },
            { "lyft",         CategoriasHormiga.TransporteMovilidad },
            { "metro",        CategoriasHormiga.TransporteMovilidad },
            { "taxi",         CategoriasHormiga.TransporteMovilidad },
            { "bip",          CategoriasHormiga.TransporteMovilidad },
            { "copec",        CategoriasHormiga.TransporteMovilidad },
            { "shell",        CategoriasHormiga.TransporteMovilidad },
            { "petrobras",    CategoriasHormiga.TransporteMovilidad },
            { "bencina",      CategoriasHormiga.TransporteMovilidad },
            { "gasolina",     CategoriasHormiga.TransporteMovilidad },

            // 📱 Suscripciones & Digital
            { "netflix",      CategoriasHormiga.SuscripcionesDigital },
            { "spotify",      CategoriasHormiga.SuscripcionesDigital },
            { "disney",       CategoriasHormiga.SuscripcionesDigital },
            { "hbo",          CategoriasHormiga.SuscripcionesDigital },
            { "apple",        CategoriasHormiga.SuscripcionesDigital },
            { "google play",  CategoriasHormiga.SuscripcionesDigital },
            { "steam",        CategoriasHormiga.SuscripcionesDigital },
            { "microsoft",    CategoriasHormiga.SuscripcionesDigital },
            { "youtube",      CategoriasHormiga.SuscripcionesDigital },
            { "prime video",  CategoriasHormiga.SuscripcionesDigital },
            { "paramount",    CategoriasHormiga.SuscripcionesDigital },
            { "twitch",       CategoriasHormiga.SuscripcionesDigital },
            { "adobe",        CategoriasHormiga.SuscripcionesDigital },
            { "canva",        CategoriasHormiga.SuscripcionesDigital },
            { "chatgpt",      CategoriasHormiga.SuscripcionesDigital },
            { "openai",       CategoriasHormiga.SuscripcionesDigital },
            { "antivirus",    CategoriasHormiga.SuscripcionesDigital },
            { "suscripcion",  CategoriasHormiga.SuscripcionesDigital },
            { "suscripción",  CategoriasHormiga.SuscripcionesDigital },
            { "membresía",    CategoriasHormiga.SuscripcionesDigital },
            { "membresia",    CategoriasHormiga.SuscripcionesDigital },

            // 💳 Financiera (gastos fijos estructurales)
            { "banco",        CategoriasHormiga.Financiera },
            { "credito",      CategoriasHormiga.Financiera },
            { "crédito",      CategoriasHormiga.Financiera },
            { "prestamo",     CategoriasHormiga.Financiera },
            { "préstamo",     CategoriasHormiga.Financiera },
            { "cuota",        CategoriasHormiga.Financiera },
            { "dividendo",    CategoriasHormiga.Financiera },
            { "arriendo",     CategoriasHormiga.Financiera },
            { "seguro",       CategoriasHormiga.Financiera },
            { "afp",          CategoriasHormiga.Financiera },
            { "isapre",       CategoriasHormiga.Financiera },
            { "fonasa",       CategoriasHormiga.Financiera },
            { "dicom",        CategoriasHormiga.Financiera },
            { "deuda",        CategoriasHormiga.Financiera },
        };

        public ClasificadorService(ApplicationDbContext context, ILogger<ClasificadorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════════
        // CAPA 1+2+3: SUGERENCIA DE CLASIFICACIÓN
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<SugerenciaClasificacionDto> SugerirClasificacionAsync(string concepto, int usuarioId)
        {
            var conceptoLower = concepto.ToLowerInvariant().Trim();

            // ── CAPA 1: Regla de Usuario (máxima prioridad) ───────────────────
            // Buscar si el usuario ya definió una regla para este concepto/comercio
            var reglaUsuario = await _context.ReglasMercante
                .Where(r => r.UsuarioId == usuarioId && conceptoLower.Contains(r.PalabraClave))
                .OrderByDescending(r => r.PalabraClave.Length) // Preferir reglas más específicas (más largas)
                .FirstOrDefaultAsync();

            if (reglaUsuario != null)
            {
                var categoriaRegla = reglaUsuario.CategoriaOverride ?? CategoriasHormiga.AlimentosAntojos;
                return new SugerenciaClasificacionDto
                {
                    CategoriasSugerida = categoriaRegla,
                    EsHormigaPorDefecto = !reglaUsuario.EsPresupuestado && CategoriasHormiga.EsHormiga(categoriaRegla),
                    Confianza = 100,
                    OrigenRegla = "ReglaUsuario",
                    EsPresupuestadoPorUsuario = reglaUsuario.EsPresupuestado
                };
            }

            // ── CAPA 2: Diccionario de Comercios ─────────────────────────────
            // Buscar la keyword más larga que coincida (más específica primero)
            var mejorMatch = _diccionarioComercios
                .Where(kv => conceptoLower.Contains(kv.Key))
                .OrderByDescending(kv => kv.Key.Length)
                .FirstOrDefault();

            if (!mejorMatch.Equals(default(KeyValuePair<string, string>)))
            {
                return new SugerenciaClasificacionDto
                {
                    CategoriasSugerida = mejorMatch.Value,
                    EsHormigaPorDefecto = CategoriasHormiga.EsHormiga(mejorMatch.Value),
                    Confianza = 80,
                    OrigenRegla = "DiccionarioComercio",
                    EsPresupuestadoPorUsuario = false
                };
            }

            // ── CAPA 3: Fallback ─────────────────────────────────────────────
            // No hay ninguna regla aplicable: sugerir Alimentos & Antojos con baja confianza
            return new SugerenciaClasificacionDto
            {
                CategoriasSugerida = CategoriasHormiga.AlimentosAntojos,
                EsHormigaPorDefecto = true,
                Confianza = 20,
                OrigenRegla = "Fallback",
                EsPresupuestadoPorUsuario = false
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // DETECCIÓN DE PATRONES (3+ transacciones en 7 días)
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task EjecutarDeteccionDePatronesAsync(int usuarioId)
        {
            // Ventana de análisis: últimos 7 días
            var fechaInicio = DateTime.UtcNow.AddDays(-7);

            // Obtener gastos recientes agrupados por categoría
            var gastosRecientes = await _context.GastosHormiga
                .Where(g => g.UsuarioId == usuarioId && g.Fecha >= fechaInicio)
                .ToListAsync();

            // Agrupar por categoría y buscar patrones (3+)
            var gruposPorCategoria = gastosRecientes
                .GroupBy(g => g.Categoria)
                .Where(grupo => grupo.Count() >= 3);

            foreach (var grupo in gruposPorCategoria)
            {
                var categoria = grupo.Key;
                var cantidad = grupo.Count();
                var montoAcumulado = grupo.Sum(g => g.CostoEstimado);

                // Solo crear alerta si no existe ya una PENDIENTE para esta categoría y período
                var alertaExistente = await _context.AlertasPatron
                    .AnyAsync(a => a.UsuarioId == usuarioId
                                && a.Categoria == categoria
                                && a.Estado == "Pendiente"
                                && a.PeriodoInicio >= fechaInicio);

                if (!alertaExistente)
                {
                    var alerta = new AlertaPatron(usuarioId, categoria, cantidad, montoAcumulado, fechaInicio);
                    _context.AlertasPatron.Add(alerta);
                    _logger.LogInformation("Alerta de patrón creada: {Categoria}, {Cantidad} transacciones, Usuario {UsuarioId}",
                        categoria, cantidad, usuarioId);
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AlertaPatronDto>> ObtenerAlertasPendientesAsync(int usuarioId)
        {
            // Retornar solo alertas pendientes de respuesta del usuario
            var alertas = await _context.AlertasPatron
                .Where(a => a.UsuarioId == usuarioId && a.Estado == "Pendiente")
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            return alertas.Select(a => new AlertaPatronDto
            {
                Id = a.Id,
                Categoria = a.Categoria,
                CantidadTransacciones = a.CantidadTransacciones,
                MontoAcumulado = a.MontoAcumulado,
                PeriodoInicio = a.PeriodoInicio,
                Estado = a.Estado
            });
        }

        // ════════════════════════════════════════════════════════════════════════
        // REGLAS DE USUARIO (Override Permanente)
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task MarcarComercioAsync(MarcarPresupuestadoDto dto, int usuarioId)
        {
            var palabraClave = dto.PalabraClave.Trim().ToLowerInvariant();

            // Buscar si ya existe una regla para esta palabra clave del usuario
            var reglaExistente = await _context.ReglasMercante
                .FirstOrDefaultAsync(r => r.UsuarioId == usuarioId && r.PalabraClave == palabraClave);

            if (reglaExistente != null)
            {
                // Si ya existe, eliminar la vieja y crear una nueva con los valores actualizados
                _context.ReglasMercante.Remove(reglaExistente);
            }

            // Crear la nueva regla de mercante
            var nuevaRegla = new ReglaMercante(usuarioId, palabraClave, dto.EsPresupuestado, dto.CategoriaOverride);
            _context.ReglasMercante.Add(nuevaRegla);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Regla de mercante guardada: {PalabraClave} → Presupuestado={EsPresupuestado}, Usuario {UsuarioId}",
                palabraClave, dto.EsPresupuestado, usuarioId);
        }

        /// <inheritdoc/>
        public async Task<bool> ResolverAlertaAsync(int alertaId, ResolverAlertaDto dto, int usuarioId)
        {
            // Buscar la alerta y verificar que pertenece al usuario (IDOR prevention)
            var alerta = await _context.AlertasPatron
                .FirstOrDefaultAsync(a => a.Id == alertaId && a.UsuarioId == usuarioId);

            if (alerta == null)
                return false;

            // Registrar la decisión del usuario en la alerta
            alerta.Resolver(dto.Decision);

            // Si el usuario dijo "No es hormiga, es presupuestado" y proporcionó el nombre del comercio,
            // guardar automáticamente la regla de mercante para no volver a preguntar
            if (dto.Decision == "MarcadaPresupuestado" && !string.IsNullOrWhiteSpace(dto.PalabraClaveComercio))
            {
                await MarcarComercioAsync(new MarcarPresupuestadoDto
                {
                    PalabraClave = dto.PalabraClaveComercio,
                    EsPresupuestado = true,
                    CategoriaOverride = alerta.Categoria
                }, usuarioId);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
