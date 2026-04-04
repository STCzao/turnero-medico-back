using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using turnero_medico_backend.Data;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Middleware;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ─── Serilog como provider de logging ────────────────────────────────────────
// Note: no bootstrap logger — avoids Serilog's ReloadableLogger.Freeze() issue
// in integration tests where Program.cs may be invoked multiple times.
builder.Host.UseSerilog((ctx, services, config) => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "turnero-medico-backend")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Turnero Médico API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá el token JWT (sin el prefijo Bearer)"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ← AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());

// ← HttpContextAccessor para acceder a User actual en servicios
builder.Services.AddHttpContextAccessor();

// ← MemoryCache para catálogos de lectura frecuente (Especialidades, ObrasSociales)
builder.Services.AddMemoryCache();

// ── Leer DATABASE_URL (Render) o ConnectionStrings__DefaultConnection ──
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration["DATABASE_URL"];

string connectionString;

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    // Render provee la URL en formato URI (postgresql://user:pass@host/db)
    // Npgsql necesita formato ADO.NET (Host=...;Database=...;Username=...;Password=...)
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432;
    connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Falta 'DATABASE_URL' o 'ConnectionStrings__DefaultConnection'.");
}

var secretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
    throw new InvalidOperationException(
        "Falta 'Jwt:SecretKey' o tiene menos de 32 bytes.");

var issuer = builder.Configuration["Jwt:Issuer"] ?? "turnero-medico-backend";
var audience = builder.Configuration["Jwt:Audience"] ?? "turnero-medico-app";

// Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// API versioning accesible para las rutas
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.HeaderApiVersionReader("api-version")
    );
}).AddMvc();

// Configurar ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configuración de contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    // Override Identity's default cookie challenge — API must return 401, not redirect
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme       = JwtBearerDefaults.AuthenticationScheme;
})
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Registrar Repository Pattern genérico
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Repositorios especializados con Include para navegaciones
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<ISecretariaRepository, SecretariaRepository>();

// Registrar Servicios
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IObraSocialService, ObraSocialService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IHorarioService, HorarioService>();
builder.Services.AddScoped<ISecretariaService, SecretariaService>();
builder.Services.AddScoped<IEspecialidadService, EspecialidadService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["db"]);

// Configurar CORS
builder.Services.AddCors(options =>
{
    // Desarrollo: permite desde localhost de React
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    // Producción: sólo el frontend desplegado
    var frontendUrl = builder.Configuration["Cors:AllowedOrigin"];
    if (!string.IsNullOrWhiteSpace(frontendUrl))
    {
        options.AddPolicy("AllowProduction", policy =>
        {
            policy
                .WithOrigins(frontendUrl)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    }
    else if (!builder.Environment.IsDevelopment())
    {
        // Si no hay URL de frontend configurada en producción, la app no debe arrancar con CORS abierto.
        throw new InvalidOperationException(
            "Falta 'Cors:AllowedOrigin' en la configuración de producción. Configure la variable de entorno correspondiente.");
    }
});

// Rate limiting: protege endpoints publicos contra abuso
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 5;            // máx 5 intentos por minuto por IP
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;             // sin cola, rechazar inmediatamente
    });
    options.AddFixedWindowLimiter("register", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(10);
        limiterOptions.PermitLimit = 5;            // máx 5 registros por 10 minutos por IP
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ← PRIMERO: Middleware para manejar excepciones globales
app.UseMiddleware<GlobalExceptionMiddleware>();

// ← Serilog request logging (después del exception middleware)
app.UseSerilogRequestLogging();

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirect in test environment (TestServer uses HTTP only)
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

// ← CORS debe ir ANTES de Authentication
var corsPolicy = app.Environment.IsDevelopment() ? "AllowReactDev" : "AllowProduction";
app.UseCors(corsPolicy);

// ← Rate limiting (skipped in Testing to avoid 429s during parallel test execution)
if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

// ← Esta línea es IMPORTANTE: Middleware de autenticación
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (ctx, rpt) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new { status = rpt.Status.ToString() });
        await ctx.Response.WriteAsync(result);
    }
});

// ← Aplicar migraciones automáticamente en producción
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "Error al aplicar migraciones automáticas. Verifique que la base de datos sea accesible.");
        throw;
    }
}

// ← Ejecutar Data Seeding (crear roles y usuario admin)
await app.SeedDatabaseAsync();

app.Run();

// Expose Program class for WebApplicationFactory (integration tests)
public partial class Program { }
