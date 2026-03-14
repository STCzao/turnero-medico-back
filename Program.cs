using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql.EntityFrameworkCore.PostgreSQL;
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

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ← AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// ← HttpContextAccessor para acceder a User actual en servicios
builder.Services.AddHttpContextAccessor();

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
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
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

// Configurar ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Configuración de contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Registrar Repository Pattern genérico
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Repositorios especializados con Include para navegaciones
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();

// Registrar Servicios
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IObraSocialService, ObraSocialService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IHorarioService, HorarioService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configurar CORS para permitir requests desde React (desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000") // Vite y Create React App
            .AllowAnyMethod()      // GET, POST, PUT, DELETE, PATCH, etc.
            .AllowAnyHeader()      // Content-Type, Authorization, etc.
            .AllowCredentials();   // Cookies, credenciales
    });
});

// Rate limiting: protege el endpoint de login contra fuerza bruta
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 5;            // máx 5 intentos por minuto por IP
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;             // sin cola, rechazar inmediatamente
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ← PRIMERO: Middleware para manejar excepciones globales
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ← CORS debe ir ANTES de Authentication
app.UseCors("AllowReactDev");

// ← Rate limiting
app.UseRateLimiter();

// ← Esta línea es IMPORTANTE: Middleware de autenticación
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ← Aplicar migraciones automáticamente en producción
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ← Ejecutar Data Seeding (crear roles y usuario admin)
await app.SeedDatabaseAsync();

app.Run();
