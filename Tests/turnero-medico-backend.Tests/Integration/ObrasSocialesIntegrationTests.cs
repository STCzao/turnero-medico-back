using System.Net;
using System.Net.Http.Json;
using turnero_medico_backend.DTOs.ObraSocialDTOs;

namespace turnero_medico_backend.Tests.Integration
{
    public class ObrasSocialesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ObrasSocialesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/obrassociales");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ConRolPaciente_Retorna200()
        {
            // GET /obrassociales no tiene restricción de rol → cualquier usuario autenticado puede
            _client.WithRole("Paciente");
            var response = await _client.GetAsync("/api/obrassociales");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            _client.WithRole("Admin");
            var response = await _client.GetAsync("/api/obrassociales/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_ConRolPaciente_Retorna403()
        {
            _client.WithRole("Paciente");
            var response = await _client.PostAsJsonAsync("/api/obrassociales",
                new ObraSocialCreateDto { Nombre = "OSDE" });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Create_ConRolAdmin_DatosValidos_Retorna201()
        {
            _client.WithRole("Admin");
            var response = await _client.PostAsJsonAsync("/api/obrassociales",
                new ObraSocialCreateDto { Nombre = "Swiss Medical" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
