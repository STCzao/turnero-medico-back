using System.Net;

namespace turnero_medico_backend.Tests.Integration
{
    public class HorariosIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HorariosIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetByDoctor_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/horarios/doctor/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetByDoctor_ConRolPaciente_Retorna403()
        {
            // GET /horarios/doctor/{id} requiere Admin, Secretaria o Doctor
            _client.WithRole("Paciente");
            var response = await _client.GetAsync("/api/horarios/doctor/1");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetByDoctor_ConRolDoctor_Retorna200ConListaVacia()
        {
            _client.WithRole("Doctor");
            var response = await _client.GetAsync("/api/horarios/doctor/1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetDisponibilidad_SinToken_Retorna401()
        {
            var response = await _client.GetAsync("/api/horarios/doctor/1/disponibilidad");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetDisponibilidad_ConRolDoctor_Retorna403()
        {
            // GET /disponibilidad requiere Admin o Secretaria — Doctor excluido
            _client.WithRole("Doctor");
            var response = await _client.GetAsync("/api/horarios/doctor/1/disponibilidad");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
