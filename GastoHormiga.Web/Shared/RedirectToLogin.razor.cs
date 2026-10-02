using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace GastoHormiga.Web.Shared
{
    /// <summary>
    /// Code-Behind para RedirectToLogin.razor.
    /// Redirige al usuario no autenticado a la página de Login.
    /// </summary>
    public partial class RedirectToLogin
    {
        // ── PARÁMETROS EN CASCADA ──────────────────────────────────────────────────
        
        /// <summary>
        /// Obtiene el estado de autenticación proporcionado por el CascadingAuthenticationState en App.razor.
        /// </summary>
        [CascadingParameter]
        private Task<AuthenticationState>? AuthState { get; set; }

        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────
        
        [Inject]
        private NavigationManager Nav { get; set; } = default!;

        // ── CICLO DE VIDA ────────────────────────────────────────────────────────────

        /// <summary>
        /// Se ejecuta al inicializar el componente.
        /// Verifica si el usuario no está autenticado y, de ser así, redirige al /login.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            var auth = await AuthState!;
            // Comprobar la identidad del usuario actual
            if (!auth.User.Identity?.IsAuthenticated ?? true)
            {
                // Redirigir suavemente al login
                Nav.NavigateTo("/Login", forceLoad: false);
            }
        }
    }
}
