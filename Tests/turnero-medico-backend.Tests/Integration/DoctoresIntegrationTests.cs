using System.Net;

namespace turnero_medico_backend.Tests.Integration
{
    public class DoctoresIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public DoctoresIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/doctores");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolPaciente_Retorna403()
        {
            // GET /doctores requiere Admin o Secretaria
            _client.WithRole("Paciente");
            var response = await _client.GetAsync("/api/doctores");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolAdmin_Retorna200ConListaVacia()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/doctores");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/doctores/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetByEspecialidad_SinDoctores_Retorna404()
        {
            // GET /doctores/especialidad/{id} devuelve 404 cuando no hay doctores para esa especialidad
            _client.WithRole("Paciente");
            var response = await _client.GetAsync("/api/doctores/especialidad/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
