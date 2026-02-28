using Microsoft.AspNetCore.Identity;

namespace turnero_medico_backend.Models.Entities
{
    /// <>
    /// Rol de la aplicación extendido de IdentityRole.
    /// Agrega descripción para documentar el propósito del rol.
    /// </>
    public class ApplicationRole : IdentityRole
    {
        /// <>
        /// Descripción del rol y sus permisos
        /// </>
        public string Descripcion { get; set; } = string.Empty;

        /// <>
        /// Constructor por defecto
        /// </>
        public ApplicationRole()
        {
        }

        /// <>
        /// Constructor con nombre de rol
        /// </>
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
