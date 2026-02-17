using Microsoft.AspNetCore.Identity;

namespace turnero_medico_backend.Models.Entities
{
    /// <summary>
    /// Rol de la aplicación extendido de IdentityRole.
    /// Agrega descripción para documentar el propósito del rol.
    /// </summary>
    public class ApplicationRole : IdentityRole
    {
        /// <summary>
        /// Descripción del rol y sus permisos
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ApplicationRole()
        {
        }

        /// <summary>
        /// Constructor con nombre de rol
        /// </summary>
        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
