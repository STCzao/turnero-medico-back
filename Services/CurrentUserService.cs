using System.Security.Claims;

namespace turnero_medico_backend.Services
{
    /// <summary>
    /// Helper para acceder a información del usuario autenticado actual
    /// </summary>
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Obtiene el ID del usuario actual
        /// </summary>
        public string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Obtiene el rol del usuario actual
        /// </summary>
        public string? GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("Rol")?.Value;
        }

        /// <summary>
        /// Verifica si el usuario actual es Admin
        /// </summary>
        public bool IsAdmin()
        {
            return GetUserRole() == "Admin";
        }

        /// <summary>
        /// Obtiene el email del usuario actual
        /// </summary>
        public string? GetUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
