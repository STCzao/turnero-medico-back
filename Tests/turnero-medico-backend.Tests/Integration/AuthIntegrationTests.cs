using System.Net;
using System.Net.Http.Json;
using turnero_medico_backend.DTOs.AuthDTOs;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Tests de integración para AuthController.
    ///
    /// AuthController es el de mayor valor para integración porque:
    ///   - Es el único que genera tokens JWT reales (no los recibe)
    ///   - Tiene [AllowAnonymous] en login/register — ningún test de Turnos cubre ese path
    ///   - Valida [DataAnnotations] del model binding (Required, EmailAddress, StringLength)
    ///   - El login real usa UserManager + PasswordHasher + SignInManager — capas que los
    ///     unitarios mockean completamente
    ///
    /// Credenciales del admin seeded (appsettings.Testing.json):
    ///   Email:    admin@test.local
    ///   Password: AdminTest123!
    /// </summary>
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        // Admin seeded — credenciales definidas en CustomWebApplicationFactory
        private const string AdminEmail    = CustomWebApplicationFactory.AdminEmail;
        private const string AdminPassword = CustomWebApplicationFactory.AdminPassword;

        public AuthIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // ── Login ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_BodyVacio_Retorna400()
        {
            // El model binding rechaza el body vacío antes de llegar al servicio.
            // Verifica que [Required] en LoginRequestDto genera 400, no 500.
            var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_EmailFormatoInvalido_Retorna400()
        {
            // [EmailAddress] en LoginRequestDto rechaza el email malformado.
            // Esto se valida en el pipeline de ASP.NET, no en el servicio.
            var dto      = new LoginRequestDto { Email = "no-es-un-email", Password = "cualquier" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_CredencialesIncorrectas_Retorna401ConMensaje()
        {
            // UserManager valida la contraseña y retorna false → el controller devuelve 401.
            // Verifica que el JSON de respuesta contiene "message" (formato de error consistente).
            var dto      = new LoginRequestDto { Email = AdminEmail, Password = "WrongPassword123" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
            var body     = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("message", body);
        }

        [Fact]
        public async Task Login_CredencialesValidas_Retorna200ConTokenJwt()
        {
            // Flujo completo: UserManager verifica hash de contraseña → genera JWT real →
            // el cliente recibe un token que puede usar en requests subsiguientes.
            // Este test cubre código que NINGÚN test unitario puede ejecutar:
            // el token generado aquí es el mismo que el frontend recibiría en producción.
            var dto      = new LoginRequestDto { Email = AdminEmail, Password = AdminPassword };
            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
            var body     = await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body.Token));
            Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
            // El token tiene el formato JWT: tres segmentos separados por puntos
            Assert.Equal(3, body.Token.Split('.').Length);
        }

        [Fact]
        public async Task Login_CredencialesValidas_TokenEsUtilizableEnEndpointProtegido()
        {
            // Verifica la cadena completa: login → token → request autenticado.
            // Si el token generado por AuthService es inválido (clave equivocada, claims faltantes),
            // este test falla aunque Login_CredencialesValidas_Retorna200ConTokenJwt pase.
            var loginDto  = new LoginRequestDto { Email = AdminEmail, Password = AdminPassword };
            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponseWrapper>();

            // Usamos el token real del login para acceder a un endpoint protegido
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.Token);

            var profileResp = await _client.GetAsync("/api/auth/profile");

            Assert.Equal(HttpStatusCode.OK, profileResp.StatusCode);
        }

        // ── Profile ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetProfile_SinToken_Retorna401()
        {
            // [Authorize] sin roles — cualquier usuario autenticado puede acceder.
            // Sin token → 401.
            var response = await _client.GetAsync("/api/auth/profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_ConToken_Retorna200ConDatosDelUsuario()
        {
            // El profile devuelve los claims del token: userId, email, nombre, rol.
            // Verifica que el controller lee correctamente los claims de User.FindFirst(...).
            var client = _client.WithRole("Admin", "test-admin-id");

            var response = await client.GetAsync("/api/auth/profile");
            var body     = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("userId", body);
            Assert.Contains("test-admin-id", body);
        }

        // ── Register Paciente ────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterPaciente_DatosValidos_Retorna200()
        {
            // Flujo completo: model binding → validaciones → UserManager.CreateAsync →
            // asigna rol Paciente → retorna mensaje.
            // Verifica que el role "Paciente" fue creado por SeedDatabaseAsync.
            var dto = new RegisterPacienteDto
            {
                Nombre          = "Juan",
                Apellido        = "García",
                Dni             = "12345678",
                Email           = "juan.garcia@test.com",
                Telefono        = "1234567890",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Password        = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-paciente", dto);
            var body     = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("message", body);
        }

        [Fact]
        public async Task RegisterPaciente_EmailInvalido_Retorna400()
        {
            // [EmailAddress] en PacienteCreateDto (base de RegisterPacienteDto) → 400.
            var dto = new RegisterPacienteDto
            {
                Nombre          = "Juan",
                Apellido        = "García",
                Dni             = "12345678",
                Email           = "no-es-email",
                Telefono        = "1234567890",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Password        = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register-paciente", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterPaciente_Luego_Login_DevuelveToken()
        {
            // Verifica el flujo completo de alta de cuenta:
            //   1. Registro exitoso
            //   2. Login con las mismas credenciales → token válido
            var email    = "nuevo.paciente@test.com";
            var password = "Paciente123!";

            var registerDto = new RegisterPacienteDto
            {
                Nombre          = "Maria",
                Apellido        = "Lopez",
                Dni             = "87654321",
                Email           = email,
                Telefono        = "0987654321",
                FechaNacimiento = new DateTime(1995, 6, 15),
                Password        = password
            };

            var registerResp = await _client.PostAsJsonAsync("/api/auth/register-paciente", registerDto);
            Assert.Equal(HttpStatusCode.OK, registerResp.StatusCode);

            var loginDto  = new LoginRequestDto { Email = email, Password = password };
            var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponseWrapper>();

            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            Assert.NotNull(loginBody?.Token);
            Assert.Equal(3, loginBody!.Token.Split('.').Length);
        }

        // ── Register Doctor / Secretaria (Admin only) ────────────────────────────

        [Fact]
        public async Task RegisterDoctor_SinAuth_Retorna401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register-doctor", new { });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RegisterDoctor_ConRolPaciente_Retorna403()
        {
            var client   = _client.WithRole("Paciente");
            var response = await client.PostAsJsonAsync("/api/auth/register-doctor", new { });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── Helper interno ───────────────────────────────────────────────────────

        /// <summary>Mapea la respuesta JSON de /api/auth/login.</summary>
        private sealed class LoginResponseWrapper
        {
            public string Token        { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string Message      { get; set; } = string.Empty;
        }
    }
}
