using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Exceptions;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Controlador exclusivo para testing de integración del GlobalExceptionMiddleware.
    /// Registrado vía AddApplicationPart en CustomWebApplicationFactory — no existe en producción.
    /// </summary>
    [ApiController]
    [Route("api/test")]
    [AllowAnonymous]
    public class TestThrowController : ControllerBase
    {
        [HttpGet("throw/{type}")]
        public IActionResult Throw(string type) => type switch
        {
            "not-found"           => throw new NotFoundException("Recurso de prueba no encontrado"),
            "conflict"            => throw new ConflictException("Conflicto de prueba"),
            "concurrency"         => throw new DbUpdateConcurrencyException("Conflicto de concurrencia de prueba"),
            "db-update"           => throw new DbUpdateException("Error de base de datos de prueba"),
            "argument-null"       => throw new ArgumentNullException("param", "Argumento nulo de prueba"),
            "invalid-operation"   => throw new InvalidOperationException("Operación inválida de prueba"),
            "unauthorized-access" => throw new UnauthorizedAccessException("Acceso denegado de prueba"),
            "key-not-found"       => throw new KeyNotFoundException("Clave no encontrada de prueba"),
            "generic"             => throw new Exception("Error genérico de prueba"),
            _                     => Ok()
        };
    }
}
