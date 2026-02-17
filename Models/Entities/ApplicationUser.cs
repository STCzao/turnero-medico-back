using Microsoft.AspNetCore.Identity;

namespace turnero_medico_backend.Models.Entities
{
    /// <summary>
    /// Usuario de la aplicación extendido de IdentityUser.
    /// Agrega propiedades personalizadas para doctor y paciente.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Nombre del usuario
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Apellido del usuario
        /// </summary>
        public string Apellido { get; set; } = string.Empty;

        /// <summary>
        /// Rol del usuario: "Paciente", "Doctor", "Admin"
        /// </summary>
        public string Rol { get; set; } = string.Empty;

        /// <summary>
        /// Relación con Doctor (si es médico)
        /// </summary>
        public int? DoctorId { get; set; }

        /// <summary>
        /// Relación con Paciente (si es paciente)
        /// </summary>
        public int? PacienteId { get; set; }

        /// <summary>
        /// Fecha de creación de la cuenta
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
