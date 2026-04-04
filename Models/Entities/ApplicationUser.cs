using Microsoft.AspNetCore.Identity;

namespace turnero_medico_backend.Models.Entities
{
    // Usuario de la aplicación extendido de IdentityUser.
    // Agrega propiedades personalizadas para doctor y paciente.
    public class ApplicationUser : IdentityUser
    {
        // Nombre del usuario
        public string Nombre { get; set; } = string.Empty;

        // Apellido del usuario
        public string Apellido { get; set; } = string.Empty;

        // Relación con Doctor (si es médico)
        public int? DoctorId { get; set; }

        // Relación con Paciente (si es paciente)
        public int? PacienteId { get; set; }


        // Fecha de creación de la cuenta
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // ─── Refresh Token ──────────────────────────────────────────
        // SHA-256 del token aleatorio emitido en cada login/refresh.
        // Nunca se almacena el token en claro.
        public string? RefreshTokenHash { get; set; }

        // Vencimiento del refresh token (30 días por defecto).
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
