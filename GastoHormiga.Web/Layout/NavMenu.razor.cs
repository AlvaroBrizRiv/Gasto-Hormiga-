using Microsoft.AspNetCore.Components;

namespace GastoHormiga.Web.Layout
{
    /// <summary>
    /// Code-Behind para NavMenu.razor (menú lateral por defecto, actualmente no se usa visiblemente si MainLayout tiene su propio Nav, pero mantenido para compatibilidad).
    /// </summary>
    public partial class NavMenu
    {
        // ── VARIABLES DE ESTADO ──────────────────────────────────────────────────────
        
        // Indica si el menú de navegación está colapsado (vista móvil)
        private bool collapseNavMenu = true;

        // ── LÓGICA DE INTERFAZ ───────────────────────────────────────────────────────

        /// <summary>
        /// Obtiene la clase CSS para aplicar el colapso del menú de navegación.
        /// </summary>
        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        /// <summary>
        /// Cambia el estado del menú entre colapsado y expandido.
        /// </summary>
        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }
    }
}
