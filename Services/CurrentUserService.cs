using System.Security.Claims;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
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
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
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

        // Obtiene el nombre completo del usuario actual
        public string? GetUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
