using System.Net;
using System.Text.Json;

namespace turnero_medico_backend.Middleware
{
    /// <summary>
    /// Middleware global que captura TODAS las excepciones no manejadas
    /// y devuelve una respuesta JSON estandarizada
    /// </summary>
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no manejada: {ExceptionMessage}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                // Excepciones específicas que reconocemos
                case ArgumentNullException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Message = "Argumentos inválidos o nulos";
                    response.Detail = exception.Message;
                    break;

                case InvalidOperationException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Message = "Operación inválida";
                    response.Detail = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.Message = "No autorizado";
                    response.Detail = exception.Message;
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Message = "Recurso no encontrado";
                    response.Detail = exception.Message;
                    break;

                // Excepción genérica para todo lo demás
                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    response.Message = "Error interno del servidor";
                    response.Detail = "Ocurrió un error inesperado. Contacta con soporte.";
#if DEBUG
                    // En desarrollo, mostrar detalles
                    response.Detail = exception.Message;
                    response.StackTrace = exception.StackTrace;
#endif
                    break;
            }

            response.Timestamp = DateTime.UtcNow;
            return context.Response.WriteAsJsonAsync(response);
        }
    }

    /// Estructura estándar para respuestas de error
    public class ErrorResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
