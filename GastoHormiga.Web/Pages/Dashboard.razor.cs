using GastoHormiga.Shared.DTOs;
using GastoHormiga.Web.Auth;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace GastoHormiga.Web.Pages
{
    /// <summary>
    /// Code-Behind para Dashboard.razor.
    /// Gestiona el estado del dashboard, llamadas a la API, motor de sugerencias y alertas de patrones.
    ///
    /// [CONEXIÓN ARQUITECTÓNICA]
    /// - Consume: api/gastoshormiga (CRUD), api/suscripciones, api/clasificador/sugerencia, api/clasificador/alertas
    /// - Recibe datos vía HttpClient con AuthorizedHttpMessageHandler (JWT inyectado automáticamente)
    /// - Notifica al motor de clasificación tras cada gasto guardado (detección de patrones)
    /// </summary>
    public partial class Dashboard
    {
        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────

        /// <summary>HttpClient pre-configurado con JWT y base URL de la API</summary>
        [Inject]
        public HttpClient Http { get; set; } = default!;

        /// <summary>Proveedor de estado de autenticación para leer datos del usuario del JWT</summary>
        [Inject]
        public CustomAuthStateProvider AuthProvider { get; set; } = default!;

        /// <summary>Permite navegar entre rutas del SPA</summary>
        [Inject]
        public NavigationManager Nav { get; set; } = default!;

        /// <summary>Logger para diagnóstico de errores en la consola del browser</summary>
        [Inject]
        public ILogger<Dashboard> Logger { get; set; } = default!;

        // ── VARIABLES DE ESTADO GENERAL ──────────────────────────────────────────────

        /// <summary>Indica si se están cargando datos iniciales (muestra spinner)</summary>
        private bool cargando = true;

        /// <summary>Indica si se está procesando una acción POST/DELETE (deshabilita botones)</summary>
        private bool guardando = false;

        /// <summary>Mensaje de error para mostrar en la UI, null si no hay error</summary>
        private string? errorMensaje;

        /// <summary>Nombre completo del usuario autenticado para el saludo del dashboard</summary>
        private string nombreUsuario = "";

        /// <summary>Código ISO de la divisa del usuario (ej: CLP, USD, ARS)</summary>
        private string divisaCodigo = "CLP";

        // ── VISIBILIDAD DE FORMULARIOS Y MODALES ─────────────────────────────────────

        /// <summary>Controla si el formulario "Agregar Gasto" está expandido</summary>
        private bool mostrarFormGasto = false;

        /// <summary>Controla si el formulario "Agregar Suscripción" está expandido</summary>
        private bool mostrarFormSub = false;

        /// <summary>Controla si el modal de importación de tarjeta está visible</summary>
        private bool mostrarModalTarjeta = false;

        // ── MODELOS DE DATOS ─────────────────────────────────────────────────────────

        /// <summary>Resumen financiero devuelto por la API (fuga mensual, anual, ahorro)</summary>
        private ResumenGastosDto resumen = new();

        /// <summary>Lista completa de gastos del usuario (hormiga y fijos)</summary>
        private List<GastoHormigaResponseDto> gastos = new();

        /// <summary>Lista de suscripciones digitales activas</summary>
        private List<SuscripcionResponseDto> suscripciones = new();

        // ── MODELOS REACTIVOS (formularios) ──────────────────────────────────────────

        /// <summary>Modelo enlazado al formulario de nuevo gasto</summary>
        private GastoHormigaCreateDto nuevoGasto = new() { Categoria = CategoriasHormiga.AlimentosAntojos };

        /// <summary>Modelo enlazado al formulario de nueva suscripción</summary>
        private SuscripcionCreateDto nuevaSub = new();

        /// <summary>Modelo del movimiento bancario simulado (modal de tarjeta)</summary>
        private GastoHormigaCreateDto movimientoTarjeta = new()
        {
            Concepto = "Compra en comercio",
            CostoEstimado = 0,
            FrecuenciaSemanal = 1,
            TipoFrecuencia = "Mensual",
            Categoria = CategoriasHormiga.AlimentosAntojos,
            OrigenTarjeta = "TarjetaDebito",
            EsImportadoTarjeta = true
        };

        // ── MOTOR DE CLASIFICACIÓN HÍBRIDO ───────────────────────────────────────────

        /// <summary>
        /// Sugerencia de categoría devuelta por el clasificador.
        /// Se actualiza en tiempo real mientras el usuario escribe el concepto del gasto.
        /// </summary>
        private SugerenciaClasificacionDto? sugerencia;

        /// <summary>
        /// Alertas de patrones pendientes de respuesta del usuario.
        /// Se muestran como banners contextuales en el Dashboard.
        /// </summary>
        private List<AlertaPatronDto> alertasPendientes = new();

        /// <summary>
        /// Temporizador para debounce en la sugerencia: espera 400ms después de que el
        /// usuario deja de escribir antes de llamar a la API del clasificador.
        /// </summary>
        private System.Timers.Timer? _debounceTimer;

        // ── LÓGICA DE CATEGORÍAS ─────────────────────────────────────────────────────

        /// <summary>Determina si un gasto individual es efectivamente hormiga (respeta override manual).</summary>
        private bool EsGastoHormigaEfectivo(GastoHormigaResponseDto g) => g.EsHormigaEfectivo;

        /// <summary>Determina si una categoría es hormiga por defecto (para badges y colores).</summary>
        private bool EsCategoriaHormiga(string cat) => CategoriasHormiga.EsHormiga(cat);

        /// <summary>
        /// Fuga mensual total = gastos hormiga (mensualizado) + suscripciones marcadas como hormiga.
        /// </summary>
        private decimal FugaMensualTotal =>
            gastos.Where(g => g.EsHormigaEfectivo).Sum(g => g.CalculoMensual)
            + suscripciones.Where(s => s.EsHormiga).Sum(s => s.CostoMensualEquivalente);

        /// <summary>
        /// Impacto anual total = gastos hormiga (anualizado) + suscripciones hormiga (anualizado).
        /// </summary>
        private decimal ImpactoAnualTotal =>
            gastos.Where(g => g.EsHormigaEfectivo).Sum(g => g.CalculoAnualizado)
            + suscripciones.Where(s => s.EsHormiga).Sum(s => s.CostoAnualEquivalente);

        private string FormatearMonto(decimal monto) =>
            monto.ToString(string.Equals(divisaCodigo, "CLP", StringComparison.OrdinalIgnoreCase) ? "N0" : "N2");

        /// <summary>Total de registros = gastos + suscripciones.</summary>
        private int RegistrosTotales => gastos.Count + suscripciones.Count;

        // ── CICLO DE VIDA DEL COMPONENTE ─────────────────────────────────────────────

        /// <summary>
        /// Inicialización del componente: carga datos del usuario desde JWT y luego llama a la API.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            // Obtener datos de sesión del JWT almacenado en LocalStorage
            var userData = await AuthProvider.ObtenerDatosUsuarioAsync();
            if (userData != null)
            {
                nombreUsuario = $"{userData.Nombre} {userData.Apellido}";
                divisaCodigo = userData.DivisaCodigo;
            }
            await CargarDatos();
        }

        // ── LLAMADAS A LA API ────────────────────────────────────────────────────────

        /// <summary>
        /// Carga en paralelo: gastos, suscripciones, resumen financiero y alertas de patrones.
        /// El paralelismo reduce el tiempo de carga inicial del dashboard.
        /// </summary>
        private async Task CargarDatos()
        {
            cargando = true;
            errorMensaje = null;
            try
            {
                // Lanzar todas las peticiones HTTP concurrentemente
                var gastosTask = Http.GetFromJsonAsync<List<GastoHormigaResponseDto>>("api/gastoshormiga");
                var subsTask = Http.GetFromJsonAsync<List<SuscripcionResponseDto>>("api/suscripciones");
                var resumenTask = Http.GetFromJsonAsync<ResumenGastosDto>("api/gastoshormiga/resumen");
                var alertasTask = Http.GetFromJsonAsync<List<AlertaPatronDto>>("api/clasificador/alertas");

                // Esperar a que todas finalicen
                await Task.WhenAll(gastosTask, subsTask, resumenTask, alertasTask);

                // Asignar resultados sin valores nulos
                gastos = await gastosTask ?? new();
                suscripciones = await subsTask ?? new();
                resumen = await resumenTask ?? new();
                alertasPendientes = await alertasTask ?? new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al cargar datos del dashboard");
                errorMensaje = $"No se pudieron cargar los datos. Error: {ex.Message}";
            }
            finally
            {
                cargando = false;
            }
        }

        // ── AUTO-SUGERENCIA DE CATEGORÍA ─────────────────────────────────────────────

        /// <summary>
        /// Se llama en tiempo real cuando el usuario escribe en el campo "Concepto".
        /// Usa debounce de 400ms para no saturar la API con cada tecla presionada.
        /// </summary>
        private void OnConceptoCambiado(ChangeEventArgs e)
        {
            nuevoGasto.Concepto = e.Value?.ToString() ?? "";
            // Cancelar el timer anterior si el usuario sigue escribiendo
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Timers.Timer(400);
            _debounceTimer.Elapsed += async (_, _) =>
            {
                _debounceTimer?.Stop();
                // Actualizar la UI en el hilo de Blazor (InvokeAsync para thread safety)
                await InvokeAsync(async () =>
                {
                    await ObtenerSugerenciaClasificacion(nuevoGasto.Concepto);
                    StateHasChanged();
                });
            };
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }

        /// <summary>
        /// Llama al endpoint GET api/clasificador/sugerencia y pre-selecciona la categoría.
        /// </summary>
        private async Task ObtenerSugerenciaClasificacion(string concepto)
        {
            if (concepto.Length < 3) // No consultar si el concepto es muy corto
            {
                sugerencia = null;
                return;
            }
            try
            {
                sugerencia = await Http.GetFromJsonAsync<SugerenciaClasificacionDto>(
                    $"api/clasificador/sugerencia?concepto={Uri.EscapeDataString(concepto)}");

                // Pre-seleccionar la categoría si la confianza es suficientemente alta
                if (sugerencia != null && sugerencia.Confianza >= 40)
                    nuevoGasto.Categoria = sugerencia.CategoriasSugerida;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error al obtener sugerencia de clasificación");
            }
        }

        // ── ACCIONES CRUD: GASTOS HORMIGA ────────────────────────────────────────────

        /// <summary>
        /// Guarda un nuevo gasto en la API.
        /// Tras el guardado, el backend detecta patrones automáticamente.
        /// </summary>
        private async Task AgregarGasto()
        {
            guardando = true;
            try
            {
                var resp = await Http.PostAsJsonAsync("api/gastoshormiga", nuevoGasto);
                if (resp.IsSuccessStatusCode)
                {
                    // Limpiar formulario y sugerencia
                    nuevoGasto = new() { Categoria = CategoriasHormiga.AlimentosAntojos };
                    sugerencia = null;
                    mostrarFormGasto = false;
                    // Recargar todo el dashboard (incluye alertas nuevas si se detectaron patrones)
                    await CargarDatos();
                }
            }
            finally { guardando = false; }
        }

        /// <summary>Elimina un gasto del usuario</summary>
        private async Task EliminarGasto(int id)
        {
            var resp = await Http.DeleteAsync($"api/gastoshormiga/{id}");
            if (resp.IsSuccessStatusCode)
                await CargarDatos();
        }

        // ── ACCIONES CRUD: SUSCRIPCIONES ─────────────────────────────────────────────

        /// <summary>Guarda una nueva suscripción en la API</summary>
        private async Task AgregarSuscripcion()
        {
            guardando = true;
            try
            {
                var resp = await Http.PostAsJsonAsync("api/suscripciones", nuevaSub);
                if (resp.IsSuccessStatusCode)
                {
                    nuevaSub = new();
                    mostrarFormSub = true;
                    await CargarDatos();
                }
            }
            finally { guardando = false; }
        }

        /// <summary>Elimina una suscripción</summary>
        private async Task EliminarSuscripcion(int id)
        {
            var resp = await Http.DeleteAsync($"api/suscripciones/{id}");
            if (resp.IsSuccessStatusCode)
                await CargarDatos();
        }

        // ── ALERTAS DE PATRONES (Human-in-the-Loop) ──────────────────────────────────

        /// <summary>
        /// El usuario acepta la alerta: confirma que esas transacciones son "gastos hormiga".
        /// El sistema las seguirá contando en la fuga mensual.
        /// </summary>
        private async Task AceptarAlertaComoHormiga(AlertaPatronDto alerta)
        {
            await ResolverAlerta(alerta, "AceptadaHormiga", null);
        }

        /// <summary>
        /// El usuario rechaza la alerta: indica que esas transacciones son gastos presupuestados.
        /// El sistema creará una ReglasMercante para no volver a preguntar sobre esa categoría.
        /// </summary>
        private async Task MarcarAlertaComoPresupuestado(AlertaPatronDto alerta)
        {
            await ResolverAlerta(alerta, "MarcadaPresupuestado", alerta.Categoria);
        }

        /// <summary>
        /// Envía la resolución de una alerta al backend y recarga los datos.
        /// </summary>
        private async Task ResolverAlerta(AlertaPatronDto alerta, string decision, string? palabraClave)
        {
            try
            {
                var dto = new ResolverAlertaDto { Decision = decision, PalabraClaveComercio = palabraClave };
                var resp = await Http.PutAsJsonAsync($"api/clasificador/alertas/{alerta.Id}/resolver", dto);
                if (resp.IsSuccessStatusCode)
                {
                    // Remover la alerta de la lista local inmediatamente para feedback visual rápido
                    alertasPendientes.Remove(alerta);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al resolver alerta {AlertaId}", alerta.Id);
            }
        }

        // ── MODAL: IMPORTAR MOVIMIENTO TARJETA ───────────────────────────────────────

        /// <summary>
        /// Abre el modal de importación bancaria simulada con datos aleatorios de comercios reales.
        /// El motor de clasificación sugiere la categoría automáticamente basado en el comercio.
        /// </summary>
        private void AbrirModalTarjeta()
        {
            var comercio = EjemplosComercios[new Random().Next(EjemplosComercios.Length)];
            movimientoTarjeta = new GastoHormigaCreateDto
            {
                Concepto = comercio,
                CostoEstimado = new decimal[] { 3990, 7500, 12990, 25000, 4500, 8990 }[new Random().Next(6)],
                FrecuenciaSemanal = 1,
                TipoFrecuencia = "Mensual",
                Categoria = CategoriasHormiga.AlimentosAntojos, // Se sobreescribirá con sugerencia
                OrigenTarjeta = "TarjetaDebito",
                EsImportadoTarjeta = true
            };
            mostrarModalTarjeta = true;
        }

        /// <summary>Cierra el modal de importación de tarjeta sin guardar</summary>
        private void CerrarModalTarjeta() => mostrarModalTarjeta = false;

        /// <summary>
        /// Confirma y guarda el movimiento bancario simulado como un gasto real.
        /// </summary>
        private async Task ConfirmarMovimientoTarjeta()
        {
            guardando = true;
            try
            {
                movimientoTarjeta.EsImportadoTarjeta = true;
                var resp = await Http.PostAsJsonAsync("api/gastoshormiga", movimientoTarjeta);
                if (resp.IsSuccessStatusCode)
                {
                    mostrarModalTarjeta = false;
                    await CargarDatos();
                }
            }
            finally { guardando = false; }
        }

        // ── DATOS ESTÁTICOS ───────────────────────────────────────────────────────────

        /// <summary>
        /// Comercios de ejemplo para la simulación de movimientos bancarios.
        /// Representan transacciones típicas que el clasificador puede reconocer.
        /// </summary>
        private static readonly string[] EjemplosComercios =
        {
            "Supermercado Lider", "Copec Estacion", "McDonald's", "Rappi Delivery",
            "Farmacia Cruz Verde", "Cine Hoyts", "Uber Eats", "Falabella.com",
            "Shell Estacion", "Starbucks Coffee", "Domino's Pizza", "Steam Store",
            "Netflix Chile", "Spotify Premium", "Uber Ride", "PedidosYa"
        };
    }
}
