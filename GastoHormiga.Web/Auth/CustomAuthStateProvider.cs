using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using GastoHormiga.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace GastoHormiga.Web.Auth
{
    /// <summary>
    /// Proveedor de estado de autenticación para Blazor WebAssembly.
    /// Lee el JWT desde LocalStorage y expone los claims al framework de autorización.
    /// Principio: Single Responsibility (SOLID-S).
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationState _estadoAnonimo;

        private const string TokenKey = "gh_auth_token";
        private const string UserDataKey = "gh_user_data";

        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            _estadoAnonimo = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync(TokenKey);

                if (string.IsNullOrWhiteSpace(token))
                    return _estadoAnonimo;

                // Parsear el token para verificar expiración
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    // Token expirado — limpiar sesión
                    await _localStorage.RemoveItemAsync(TokenKey);
                    await _localStorage.RemoveItemAsync(UserDataKey);
                    return _estadoAnonimo;
                }

                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                var usuario = new ClaimsPrincipal(identity);

                return new AuthenticationState(usuario);
            }
            catch
            {
                return _estadoAnonimo;
            }
        }

        /// <summary>
        /// Notifica al framework que el usuario inició sesión correctamente.
        /// </summary>
        public async Task MarcarComoAutenticado(AuthResponseDto respuesta)
        {
            await _localStorage.SetItemAsStringAsync(TokenKey, respuesta.Token);
            await _localStorage.SetItemAsync(UserDataKey, respuesta);

            var claims = ParseClaimsFromJwt(respuesta.Token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var usuario = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(usuario)));
        }

        /// <summary>
        /// Cierra la sesión del usuario eliminando el token del almacenamiento local.
        /// </summary>
        public async Task CerrarSesion()
        {
            await _localStorage.RemoveItemAsync(TokenKey);
            await _localStorage.RemoveItemAsync(UserDataKey);
            NotifyAuthenticationStateChanged(Task.FromResult(_estadoAnonimo));
        }

        /// <summary>
        /// Obtiene el token JWT almacenado (para adjuntarlo a las peticiones HTTP).
        /// </summary>
        public async Task<string?> ObtenerTokenAsync()
        {
            return await _localStorage.GetItemAsStringAsync(TokenKey);
        }

        /// <summary>
        /// Obtiene los datos del usuario desde LocalStorage.
        /// </summary>
        public async Task<AuthResponseDto?> ObtenerDatosUsuarioAsync()
        {
            return await _localStorage.GetItemAsync<AuthResponseDto>(UserDataKey);
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
    }
}
