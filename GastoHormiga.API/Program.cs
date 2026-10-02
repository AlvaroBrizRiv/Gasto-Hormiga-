/// <summary>
/// ── [CONEXIÓN ARQUITECTÓNICA: GASTO HORMIGA API] ──────────────────────────
/// Este archivo (Program.cs) es el punto de entrada (Entry Point) del Backend.
/// Aquí se realiza la Inyección de Dependencias (DI) uniendo todas las capas:
/// 1. EF Core (ApplicationDbContext) -> Capa de Datos (SQL Server).
/// 2. JWT Authentication -> Seguridad y prevención de acceso no autorizado.
/// 3. CORS -> Permite que el Frontend (Blazor WebAssembly) consuma la API.
/// 4. Servicios (IGastoHormigaService, IAuthService) -> Capa de Lógica de Negocio.
/// 5. Middleware Pipeline -> Manejo global de errores, Rate Limiting (OWASP), Autenticación.
/// ────────────────────────────────────────────────────────────────────────────
/// </summary>
using System.Text;
using System.Threading.RateLimiting;
using GastoHormiga.API.Data;
using GastoHormiga.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GastoHormiga API",
        Version = "v1",
        Description = "API para el control de gastos hormiga y suscripciones.",
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT. Ejemplo: Bearer {tu_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwtConfig = builder.Configuration.GetSection("Jwt");
var secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
var issuer = jwtConfig["Issuer"] ?? "GastoHormigaAPI";
var audience = jwtConfig["Audience"] ?? "GastoHormigaWeb";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaCors", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7001",
                "http://localhost:5001",
                "https://localhost:7260",
                "http://localhost:5046",
                "https://localhost:7230",
                "http://localhost:5292",
                "https://localhost:7107",
                "http://localhost:5158"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    
    options.RejectionStatusCode = 429;
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGastoHormigaService, GastoHormigaService>();
builder.Services.AddScoped<ISuscripcionService, SuscripcionService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
// Motor de clasificación híbrido (Human-in-the-Loop)
builder.Services.AddScoped<IClasificadorService, ClasificadorService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            mensaje = "Ocurrio un error interno. Por favor intenta de nuevo mas tarde."
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GastoHormiga API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("PoliticaCors");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try { db.Database.Migrate(); }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones.");
    }
}

app.Run();
