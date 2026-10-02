using GastoHormiga.Web.Auth;
using Microsoft.AspNetCore.Components;

namespace GastoHormiga.Web.Layout
{
    /// <summary>
    /// Code-Behind para el diseño principal de la aplicación (MainLayout).
    /// Controla la navegación global y el estado de la sesión activa en el navbar.
    /// </summary>
    public partial class MainLayout : LayoutComponentBase
    {
        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────
        
        [Inject]
        public NavigationManager Nav { get; set; } = default!;
        
        [Inject]
        public CustomAuthStateProvider AuthProvider { get; set; } = default!;

        // ── VARIABLES DE ESTADO ──────────────────────────────────────────────────────

        // Controla si el menú en vista móvil (hamburger menu) está desplegado o no
        private bool menuAbierto = false;

        // ── LÓGICA DE INTERFAZ ───────────────────────────────────────────────────────

        /// <summary>
        /// Cambia el estado del menú hamburguesa (lo abre si está cerrado y viceversa).
        /// </summary>
        private void ToggleMenu() => menuAbierto = !menuAbierto;

        // ── LÓGICA DE AUTENTICACIÓN ──────────────────────────────────────────────────

        /// <summary>
        /// Cierra la sesión del usuario actual borrando su token y forzando la recarga de la app.
        /// </summary>
        private async Task CerrarSesion()
        {
            // Ocultar el menú móvil por si estaba abierto
            menuAbierto = false;
            
            // Llamar al proveedor de autenticación para que elimine el JWT de LocalStorage
            await AuthProvider.CerrarSesion();
            
            // Navegar a la página de inicio (Landing Page) y forzar recarga (F5 virtual) para limpiar estados residuales
            Nav.NavigateTo("/", forceLoad: true);
        }
    }
}
