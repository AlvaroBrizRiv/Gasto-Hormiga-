using GastoHormiga.Shared.DTOs;
using GastoHormiga.Web.Auth;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace GastoHormiga.Web.Pages
{
    /// <summary>
    /// Code-Behind para MisGastos.razor.
    /// Contiene la lógica para obtener el listado histórico de gastos, filtrarlos y ordenarlos.
    /// </summary>
    public partial class MisGastos
    {
        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────
        
        [Inject]
        public HttpClient Http { get; set; } = default!;
        
        [Inject]
        public CustomAuthStateProvider AuthProvider { get; set; } = default!;

        // ── VARIABLES DE ESTADO ──────────────────────────────────────────────────────

        // Controla la visibilidad del spinner de carga
        private bool cargando = true;
        
        // Almacena un mensaje de error si falla la comunicación con la API
        private string? errorMensaje;
        
        // Código de la moneda local del usuario (ej. CLP)
        private string divisaCodigo = "CLP";
        
        // Filtro seleccionado por el usuario. Vacío ("") significa "Todas"
        private string filtroCategoria = "";
        
        // Criterio de ordenamiento seleccionado por el usuario. Por defecto: Más recientes primero
        private string ordenamiento = "fecha-desc";
        
        // Lista original sin filtrar obtenida desde la API
        private List<GastoHormigaResponseDto> gastos = new();

        // ── LÓGICA DE CATEGORÍAS ─────────────────────────────────────────────────────

        /// <summary>
        /// Determina si un gasto es hormiga efectivo, respetando el override manual del usuario.
        /// Usa EsHormigaEfectivo del DTO en vez de comparar categorías directamente.
        /// </summary>
        private bool EsCategoriaHormiga(string cat) => CategoriasHormiga.EsHormiga(cat);

        private string FormatearMonto(decimal monto) =>
            monto.ToString(string.Equals(divisaCodigo, "CLP", StringComparison.OrdinalIgnoreCase) ? "N0" : "N2");

        // ── PROPIEDADES CALCULADAS (REACTIVIDAD) ─────────────────────────────────────

        /// <summary>
        /// Propiedad calculada que se recalcula automáticamente cuando Blazor detecta cambios en 'filtroCategoria' u 'ordenamiento'.
        /// Devuelve una secuencia inmutable de los gastos procesados.
        /// </summary>
        private IEnumerable<GastoHormigaResponseDto> GastosFiltrados =>
            // Primero se aplica el filtro por categoría (Where) si el usuario seleccionó una
            (string.IsNullOrEmpty(filtroCategoria) ? gastos : gastos.Where(g => g.Categoria == filtroCategoria))
            // Luego se aplica el ordenamiento (OrderBy) dependiendo del criterio elegido
            .OrderBy(g => ordenamiento switch
            {
                "fecha-asc" => (object)g.Fecha,                           // Más antiguos primero
                "costo-desc" => (object)(-g.CostoEstimado),               // Mayor costo primero (negativo invierte el orden ascendente)
                "costo-asc" => (object)g.CostoEstimado,                   // Menor costo primero
                _ => (object)(-g.Fecha.Ticks)                             // Default: Más recientes primero (fecha-desc)
            });

        // ── CICLO DE VIDA DEL COMPONENTE ─────────────────────────────────────────────

        /// <summary>
        /// Se ejecuta al cargar el componente. Obtiene los datos del usuario y carga la lista de gastos.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            // Extraer la moneda preferida del usuario desde el JWT decodificado en LocalStorage
            var userData = await AuthProvider.ObtenerDatosUsuarioAsync();
            if (userData != null)
                divisaCodigo = userData.DivisaCodigo;

            // Iniciar la carga de los datos desde la API
            await CargarGastos();
        }

        // ── LLAMADAS A LA API ────────────────────────────────────────────────────────

        /// <summary>
        /// Solicita al backend el historial completo de gastos del usuario autenticado.
        /// </summary>
        private async Task CargarGastos()
        {
            cargando = true;
            errorMensaje = null;
            try
            {
                // Realiza un GET a la API y deserializa directamente en una lista. Si es null, devuelve una lista vacía.
                gastos = await Http.GetFromJsonAsync<List<GastoHormigaResponseDto>>("api/gastoshormiga") ?? new();
            }
            catch
            {
                errorMensaje = "No se pudieron cargar los gastos.";
            }
            finally 
            { 
                cargando = false; 
            }
        }
    }
}
