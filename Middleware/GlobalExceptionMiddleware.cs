using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace turnero_medico_backend.Middleware
{
    // Middleware global que captura TODAS las excepciones no manejadas y devuelve una respuesta JSON estandarizada
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
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                await HandleExceptionAsync(context, ex, env);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment env)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                // Excepciones específicas que reconocemos
                case DbUpdateConcurrencyException:
                    // Conflicto de concurrencia optimista (RowVersion): otro proceso modificó el registro primero.
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    response.StatusCode = HttpStatusCode.Conflict;
                    response.Message = "Conflicto de concurrencia";
                    response.Detail = "El recurso fue modificado por otro proceso. Reintente la operación.";
                    break;

                case DbUpdateException dbEx:
                    // Error de base de datos: discriminamos entre unique constraint y FK violation
                    // usando el código de error PostgreSQL del inner exception (23505 = unique, 23503 = FK).
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    response.StatusCode = HttpStatusCode.Conflict;
                    var innerMsg = dbEx.InnerException?.Message ?? string.Empty;
                    if (innerMsg.Contains("23505") || innerMsg.Contains("duplicate key"))
                    {
                        response.Message = "Dato duplicado";
                        response.Detail = "Ya existe un registro con ese valor. Verifique los datos ingresados (por ejemplo, el DNI).";
                    }
                    else if (innerMsg.Contains("23503") || innerMsg.Contains("foreign key"))
                    {
                        response.Message = "Referencia inválida";
                        response.Detail = "Uno de los valores de referencia no existe (por ejemplo, la Obra Social indicada).";
                    }
                    else
                    {
                        response.Message = "Conflicto de integridad";
                        response.Detail = "No se puede completar la operación porque el recurso tiene datos asociados o violaría una restricción única.";
                    }
                    break;

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
                    // 403 Forbidden: el usuario está autenticado pero no tiene permiso.
                    // 401 Unauthorized se reserva para peticiones sin credenciales válidas (lo maneja el middleware de JWT).
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.Message = "Acceso denegado";
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
                    if (env.IsDevelopment())
                    {
                        // En desarrollo, mostrar detalles
                        response.Detail = exception.Message;
                        response.StackTrace = exception.StackTrace;
                    }
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
