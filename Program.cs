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

// Configurar Entity Framework con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Configurar autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = builder.Configuration["Jwt:SecretKey"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

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

// Registrar Servicios
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IObraSocialService, ObraSocialService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<CurrentUserService>();

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

// ← Ejecutar Data Seeding (crear roles y usuario admin)
await app.SeedDatabaseAsync();

app.Run();
