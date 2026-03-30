namespace turnero_medico_backend.Services.Interfaces
{
    // Abstrae el acceso a las claims del usuario autenticado en el request actual.
    // Se inyecta en servicios para que las reglas de negocio puedan aplicar autorización
    // sin depender directamente de HttpContext (facilita testing).
    // Todos los métodos devuelven null si no hay usuario autenticado o si la claim no existe.
    public interface ICurrentUserService
    {
        string? GetUserId();    // ClaimTypes.NameIdentifier — ID de AspNetUsers
        string? GetUserRole();  // ClaimTypes.Role — primer rol del usuario
        bool IsAdmin();         // Atajo para GetUserRole() == "Admin"
        string? GetUserEmail(); // ClaimTypes.Email
        string? GetUserName();  // ClaimTypes.Name — "Nombre Apellido"
    }
}
