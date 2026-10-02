/// <summary>
/// ── [CONEXIÓN ARQUITECTÓNICA: GASTO HORMIGA WEB (BLAZOR)] ─────────────────
/// Este archivo (Program.cs) es el punto de entrada del Frontend SPA (Single Page Application).
/// Aquí se configuran los cimientos para que Blazor se comunique con la API:
/// 1. HttpClient Factory -> Configurado apuntando al Backend (GastoHormigaAPI).
/// 2. Autenticación (CustomAuthStateProvider) -> Gestiona el JWT en el LocalStorage y sincroniza el estado de sesión de UI.
/// 3. AuthorizedHttpMessageHandler -> Intercepta cada petición HTTP del cliente e inyecta el token JWT (Bearer) en las cabeceras.
/// ────────────────────────────────────────────────────────────────────────────
/// </summary>
using Blazored.LocalStorage;
using GastoHormiga.Web;
using GastoHormiga.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL base de la API — configurable desde wwwroot/appsettings.json sin recompilar
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7107/";

// Autorizacion y estado de sesion (registrar ANTES del HttpClient para que el handler lo use)
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());

// Handler que inyecta automaticamente el JWT en cada peticion
builder.Services.AddTransient<AuthorizedHttpMessageHandler>();

// HttpClient con URL de la API y el handler de autorizacion
builder.Services.AddHttpClient("GastoHormigaAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthorizedHttpMessageHandler>();

// Registrar el HttpClient como servicio Scoped para inyeccion en componentes
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("GastoHormigaAPI"));

await builder.Build().RunAsync();
