using System.Net;
using System.Net.Http.Json;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.TurnoDTOs;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Tests de integración para TurnosController.
    /// Cada test ejercita el pipeline HTTP completo:
    ///   JWT middleware → RBAC → routing → controller → service → DB → serialización JSON
    ///
    /// Lo que estos tests cubren y los unitarios NO pueden cubrir:
    ///   - El [Authorize] real de ASP.NET (no un mock)
    ///   - El routing de [Route] / [HttpGet] / [HttpPost]
    ///   - La deserialización del request body
    ///   - El GlobalExceptionMiddleware mapeando excepciones a status codes
    ///   - La serialización JSON de la respuesta
    /// </summary>
    public class TurnosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TurnosIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // ── Autenticación y autorización ────────────────────────────────────────

        [Fact]
        public async Task GetAll_SinToken_Retorna401()
        {
            // El middleware de JWT no encuentra Authorization header → 401
            // Ningún test unitario puede probar esto: los unitarios mockean el servicio,
            // nunca pasan por el middleware real de autenticación.
            var response = await _client.GetAsync("/api/turnos");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolPaciente_Retorna403()
        {
            // [Authorize(Roles = "Admin,Secretaria")] bloquea al Paciente → 403
            // Los unitarios testean la lógica del servicio, nunca el [Authorize] del controller.
            var client = _client.WithRole("Paciente");

            var response = await client.GetAsync("/api/turnos");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolAdmin_Retorna200ConListaVacia()
        {
            // Happy path completo: token válido → rol correcto → controller → servicio → 200
            // Verifica además que la respuesta es JSON deserializable al DTO esperado.
            var client = _client.WithRole("Admin");

            var response = await client.GetAsync("/api/turnos");
            var body     = await response.Content.ReadFromJsonAsync<PagedResultDto<TurnoReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.Equal(0, body.Total);
        }

        [Fact]
        public async Task GetAll_ConRolSecretaria_Retorna200()
        {
            // Secretaria también tiene permiso en [Authorize(Roles = "Admin,Secretaria")]
            var client = _client.WithRole("Secretaria");

            var response = await client.GetAsync("/api/turnos");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // ── Validación de parámetros ────────────────────────────────────────────

        [Fact]
        public async Task GetAll_EstadoInvalido_Retorna400ConMensaje()
        {
            // El controller valida ?estado antes de llamar al servicio → 400
            // Este test valida que la lógica de ValidarEstado() dentro del controller
            // realmente se ejecuta en el pipeline HTTP y devuelve JSON con el mensaje esperado.
            var client = _client.WithRole("Admin");

            var response = await client.GetAsync("/api/turnos?estado=NoExiste");
            var body     = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Estado", body);
            Assert.Contains("NoExiste", body);
        }

        // ── Manejo global de errores ────────────────────────────────────────────

        [Fact]
        public async Task GetById_TurnoInexistente_Retorna404()
        {
            // El controller hace: if (turno == null) return NotFound(...)
            // Verifica que el routing GET /api/turnos/{id} funciona y que
            // el NotFound se serializa correctamente.
            var client = _client.WithRole("Admin");

            var response = await client.GetAsync("/api/turnos/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_IdMismatch_Retorna400()
        {
            // PATCH /api/turnos/{id} valida que id == dto.Id antes de llamar al servicio
            // Esta validación vive en el controller, no en el servicio — los unitarios no la tocan.
            var client = _client.WithRole("Doctor");

            var dto      = new TurnoUpdateDto { Id = 1, Estado = "Completado" };
            var response = await client.PatchAsJsonAsync("/api/turnos/99", dto);  // id URL ≠ dto.Id
            var body     = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("ID", body);
        }

        // ── Routing ────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetPendientes_ConRolPaciente_Retorna403()
        {
            // Verifica que el routing GET /api/turnos/pendientes llega al action correcto
            // (y no al GetById con id="pendientes") y que el [Authorize(Roles="Secretaria,Admin")]
            // funciona sobre esa ruta específica.
            var client = _client.WithRole("Paciente");

            var response = await client.GetAsync("/api/turnos/pendientes");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetPendientes_ConRolAdmin_Retorna200()
        {
            var client = _client.WithRole("Admin");

            var response = await client.GetAsync("/api/turnos/pendientes");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
