using System.Security.Claims;

namespace turnero_medico_backend.Services
{
    // Helper para acceder a información del usuario autenticado actual   
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        // Obtiene el ID del usuario actual        
        public string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // Obtiene el rol del usuario actual

        public string? GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Rol")?.Value;
        }

        // Verifica si el usuario actual es Admin
        public bool IsAdmin()
        {
            return GetUserRole() == "Admin";
        }


        // Obtiene el email del usuario actual
        public string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
