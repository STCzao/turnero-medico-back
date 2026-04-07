using System.Net;
using System.Net.Http.Json;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Tests de integración para GlobalExceptionMiddleware.
    ///
    /// Usa TestThrowController (/api/test/throw/{type}) para disparar cada tipo de excepción
    /// y verifica que el middleware las mapea al status code y mensaje correctos.
    /// El endpoint es [AllowAnonymous] — no se necesita JWT para estos tests.
    /// </summary>
    public class MiddlewareIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public MiddlewareIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private sealed class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public string Detail  { get; set; } = string.Empty;
        }

        [Fact]
        public async Task NotFoundException_Retorna404ConMensaje()
        {
            var response = await _client.GetAsync("/api/test/throw/not-found");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Recurso no encontrado", body!.Message);
        }

        [Fact]
        public async Task ConflictException_Retorna409ConMensaje()
        {
            var response = await _client.GetAsync("/api/test/throw/conflict");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Conflicto", body!.Message);
        }

        [Fact]
        public async Task DbUpdateConcurrencyException_Retorna409ConflictoConcurrencia()
        {
            var response = await _client.GetAsync("/api/test/throw/concurrency");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Conflicto de concurrencia", body!.Message);
        }

        [Fact]
        public async Task DbUpdateException_Retorna409ConflictoIntegridad()
        {
            var response = await _client.GetAsync("/api/test/throw/db-update");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Conflicto de integridad", body!.Message);
        }

        [Fact]
        public async Task ArgumentNullException_Retorna400()
        {
            var response = await _client.GetAsync("/api/test/throw/argument-null");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Argumentos inválidos o nulos", body!.Message);
        }

        [Fact]
        public async Task InvalidOperationException_Retorna400()
        {
            var response = await _client.GetAsync("/api/test/throw/invalid-operation");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Operación inválida", body!.Message);
        }

        [Fact]
        public async Task UnauthorizedAccessException_Retorna403()
        {
            var response = await _client.GetAsync("/api/test/throw/unauthorized-access");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Acceso denegado", body!.Message);
        }

        [Fact]
        public async Task KeyNotFoundException_Retorna404()
        {
            var response = await _client.GetAsync("/api/test/throw/key-not-found");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Recurso no encontrado", body!.Message);
        }

        [Fact]
        public async Task ExcepcionGenerica_Retorna500()
        {
            var response = await _client.GetAsync("/api/test/throw/generic");
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("Error interno del servidor", body!.Message);
        }
    }
}
