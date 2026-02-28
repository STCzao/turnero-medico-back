using Microsoft.AspNetCore.Identity;

namespace turnero_medico_backend.Models.Entities
{
    /// <>
    /// Usuario de la aplicación extendido de IdentityUser.
    /// Agrega propiedades personalizadas para doctor y paciente.
    /// </>
    public class ApplicationUser : IdentityUser
    {
        /// <>
        /// Nombre del usuario
        /// </>
        public string Nombre { get; set; } = string.Empty;

        /// <>
        /// Apellido del usuario
        /// </>
        public string Apellido { get; set; } = string.Empty;

        /// <>
        /// Rol del usuario: "Paciente", "Doctor", "Admin"
        /// </>
        public string Rol { get; set; } = string.Empty;

        /// <>
        /// Relación con Doctor (si es médico)
        /// </>
        public int? DoctorId { get; set; }

        /// <>
        /// Relación con Paciente (si es paciente)
        /// </>
        public int? PacienteId { get; set; }

        /// <>
        /// Fecha de creación de la cuenta
        /// </>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
