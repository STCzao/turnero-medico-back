using System.Net;

namespace turnero_medico_backend.Tests.Integration
{
    public class PacientesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PacientesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/pacientes");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolDoctor_Retorna403()
        {
            // GET /pacientes requiere Admin o Secretaria
            _client.WithRole("Doctor");
            var response = await _client.GetAsync("/api/pacientes");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolAdmin_Retorna200ConListaVacia()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/pacientes");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/pacientes/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
