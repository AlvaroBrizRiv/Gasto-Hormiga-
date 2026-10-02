using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace GastoHormiga.Web.Pages
{
    /// <summary>
    /// Code-Behind para Registro.razor.
    /// Contiene la lógica del formulario multi-paso, creación de cuenta y verificación OTP.
    /// </summary>
    public partial class Registro
    {
        // ── INYECCIÓN DE DEPENDENCIAS ────────────────────────────────────────────────
        
        [Inject]
        public HttpClient Http { get; set; } = default!;
        
        [Inject]
        public NavigationManager Nav { get; set; } = default!;

        // ── VARIABLES DE ESTADO ──────────────────────────────────────────────────────

        // Modelo de datos para el formulario de registro (Paso 1)
        // Valores por defecto: Chile (CL) y Peso Chileno (CLP)
        private RegistroUsuarioDto registroDto = new() { CodigoPais = "CL", DivisaCodigo = "CLP" };
        
        // Controla la vista actual del formulario multi-paso (1=Datos, 2=OTP, 3=Éxito)
        private int paso = 1;
        
        // Controla si se está procesando una solicitud para bloquear los botones
        private bool cargando = false;
        
        // Mensaje de error general a mostrar en la UI
        private string? errorMensaje;
        
        // Valor del input donde el usuario ingresa el código de 6 dígitos (OTP)
        private string codigoOtp = string.Empty;

        // ── LÓGICA DE INTERFAZ ───────────────────────────────────────────────────────

        /// <summary>
        /// Actualiza automáticamente la divisa (moneda) dependiendo del país seleccionado.
        /// Se dispara con el evento @oninput del select de países.
        /// </summary>
        private void ActualizarDivisa(ChangeEventArgs e)
        {
            var pais = e.Value?.ToString() ?? "CL";
            registroDto.CodigoPais = pais;
            
            // Mapeo simple de código de país a código de moneda ISO
            registroDto.DivisaCodigo = pais switch
            {
                "AR" => "ARS", // Peso Argentino
                "MX" => "MXN", // Peso Mexicano
                "PE" => "PEN", // Sol Peruano
                "CO" => "COP", // Peso Colombiano
                "US" => "USD", // Dólar Estadounidense
                _ => "CLP"     // Peso Chileno (Por defecto)
            };
        }

        // ── PASO 1: REGISTRO DE DATOS ────────────────────────────────────────────────

        /// <summary>
        /// Envía los datos del nuevo usuario a la API. Si es exitoso, avanza al paso 2 (OTP).
        /// </summary>
        private async Task HandleRegistro()
        {
            cargando = true;
            errorMensaje = null;

            try
            {
                // Realizar petición POST al endpoint de registro
                var resp = await Http.PostAsJsonAsync("api/auth/registrar", registroDto);

                if (resp.IsSuccessStatusCode)
                {
                    // Éxito: el backend generó la cuenta (inactiva) y envió el SMS (mock).
                    // Avanzamos al paso 2
                    paso = 2;
                }
                else if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // Status 409 Conflict: El correo ya existe
                    var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
                    errorMensaje = err?.Mensaje ?? "Ya existe una cuenta con esos datos.";
                }
                else
                {
                    errorMensaje = "Error al registrar. Intenta nuevamente.";
                }
            }
            catch
            {
                // Error de red o API caída
                errorMensaje = "No se pudo conectar con el servidor.";
            }
            finally
            {
                cargando = false;
            }
        }

        // ── PASO 2: VERIFICACIÓN OTP ─────────────────────────────────────────────────

        /// <summary>
        /// Envía el código ingresado por el usuario para ser validado en el backend.
        /// </summary>
        private async Task HandleVerificarOtp()
        {
            cargando = true;
            errorMensaje = null;

            try
            {
                // Construir el DTO necesario para la verificación
                var dto = new VerificarOtpDto { Email = registroDto.Email, CodigoOtp = codigoOtp };
                
                // POST para verificar código
                var resp = await Http.PostAsJsonAsync("api/auth/verificar-otp", dto);

                if (resp.IsSuccessStatusCode)
                {
                    // Éxito: El código es válido, la cuenta está activa.
                    // Avanzamos al paso 3 (Éxito)
                    paso = 3;
                }
                else
                {
                    // Fallo: Código incorrecto o expiró
                    var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
                    errorMensaje = err?.Mensaje ?? "Codigo incorrecto o expirado.";
                }
            }
            catch
            {
                errorMensaje = "Error al verificar el codigo.";
            }
            finally
            {
                cargando = false;
            }
        }

        /// <summary>
        /// Solicita al backend la generación y envío de un nuevo código OTP.
        /// </summary>
        private async Task ReenviarOtp()
        {
            cargando = true;
            errorMensaje = null;

            try
            {
                // POST para reenviar OTP usando el email registrado en el paso 1
                var resp = await Http.PostAsJsonAsync("api/auth/reenviar-otp", new { Email = registroDto.Email });
                
                if (!resp.IsSuccessStatusCode)
                    errorMensaje = "No se pudo reenviar el codigo.";
            }
            catch
            {
                errorMensaje = "Error de conexion.";
            }
            finally
            {
                cargando = false;
            }
        }

        // ── CLASES AUXILIARES ────────────────────────────────────────────────────────

        /// <summary>
        /// Clase para deserializar los errores devueltos por la API.
        /// </summary>
        private class ErrorResponse
        {
            public string? Mensaje { get; set; }
        }
    }
}
