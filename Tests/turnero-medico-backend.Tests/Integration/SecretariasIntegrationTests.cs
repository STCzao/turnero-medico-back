using System.Net;

namespace turnero_medico_backend.Tests.Integration
{
    public class SecretariasIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SecretariasIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/secretarias");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolPaciente_Retorna403()
        {
            // SecretariasController tiene [Authorize(Roles = "Admin")] a nivel de clase
            _client.WithRole("Paciente");
            var response = await _client.GetAsync("/api/secretarias");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolAdmin_Retorna200ConListaVacia()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/secretarias");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_ConRolAdmin_Inexistente_Retorna404()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/secretarias/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
