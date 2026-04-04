namespace turnero_medico_backend.Exceptions
{
    /// Recurso no encontrado (404). Lanzar en lugar de retornar null cuando la ausencia
    /// del recurso es un error de negocio (no un resultado opcional).
    public class NotFoundException(string message) : Exception(message);

    /// Conflicto de estado de negocio (409). Lanzar cuando la operación no puede
    /// completarse por el estado actual del recurso (distinto de conflicto de concurrencia DB).
    public class ConflictException(string message) : Exception(message);
}
