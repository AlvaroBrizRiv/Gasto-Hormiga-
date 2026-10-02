using Blazored.LocalStorage;

namespace GastoHormiga.Web.Auth
{
    /// <summary>
    /// DelegatingHandler que adjunta automáticamente el token JWT Bearer
    /// en cada petición HTTP saliente del HttpClient.
    /// Principio: Single Responsibility (SOLID-S) — centraliza la inyección del token.
    /// Evita que cada página tenga que adjuntar el header manualmente.
    /// </summary>
    public class AuthorizedHttpMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private const string TokenKey = "gh_auth_token";

        public AuthorizedHttpMessageHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Solo adjunta el token si no está ya presente (no sobreescribir login/registro)
            if (request.Headers.Authorization == null)
            {
                var token = await _localStorage.GetItemAsStringAsync(TokenKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    token = token.Trim('"'); // Limpiar comillas si existieran por serialización
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
