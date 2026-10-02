using GastoHormiga.Shared.DTOs;
using GastoHormiga.Web.Auth;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace GastoHormiga.Web.Pages
{
    /// <summary>
    /// Code-Behind para Login.razor.
    /// Contiene la lógica de autenticación y comunicación con el backend.
    /// </summary>
    public partial class Login
    {
        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────
        
        [Inject]
        public HttpClient Http { get; set; } = default!;
        
        [Inject]
        public NavigationManager Nav { get; set; } = default!;
        
        [Inject]
        public CustomAuthStateProvider AuthProvider { get; set; } = default!;

        // ── VARIABLES DE ESTADO ──────────────────────────────────────────────────────

        // Modelo de datos enlazado al formulario de inicio de sesión
        private LoginDto loginDto = new();
        
        // Controla si se está procesando la solicitud para deshabilitar el botón
        private bool cargando = false;
        
        // Mensaje de error a mostrar en caso de fallo
        private string? errorMensaje;
        
        // Mensaje de éxito a mostrar en caso de operación correcta (usualmente redirige de inmediato)
        private string? mensajeExito;

        // ── LÓGICA PRINCIPAL ─────────────────────────────────────────────────────────

        /// <summary>
        /// Maneja el envío del formulario de inicio de sesión.
        /// </summary>
        private async Task HandleLogin()
        {
            // Bloquea el botón y limpia mensajes previos
            cargando = true;
            errorMensaje = null;
            mensajeExito = null;

            try
            {
                // Enviar la petición POST con las credenciales al endpoint de login
                var respuesta = await Http.PostAsJsonAsync("api/auth/login", loginDto);

                if (respuesta.IsSuccessStatusCode)
                {
                    // Si el login es exitoso, extraer los datos del JWT y el usuario
                    var authData = await respuesta.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (authData != null)
                    {
                        // Guardar el JWT en LocalStorage y notificar al sistema que estamos logueados
                        await AuthProvider.MarcarComoAutenticado(authData);
                        
                        // Redirigir al usuario al dashboard principal
                        Nav.NavigateTo("/dashboard");
                    }
                }
                else
                {
                    // Si falla, leer el mensaje de error del backend (ej. "Contraseña incorrecta")
                    var error = await respuesta.Content.ReadFromJsonAsync<ErrorResponse>();
                    errorMensaje = error?.Mensaje ?? "Credenciales incorrectas. Verifica tu email y contrasena.";
                }
            }
            catch
            {
                // Si hay una excepción, probablemente el servidor esté caído o haya un error de red
                errorMensaje = "No se pudo conectar con el servidor. Verifica que la API este encendida.";
            }
            finally
            {
                // Desbloquear el botón de envío
                cargando = false;
            }
        }

        // ── CLASES AUXILIARES ────────────────────────────────────────────────────────

        /// <summary>
        /// Clase auxiliar para mapear el formato estándar de errores del backend.
        /// </summary>
        private class ErrorResponse
        {
            public string? Mensaje { get; set; }
        }
    }
}
