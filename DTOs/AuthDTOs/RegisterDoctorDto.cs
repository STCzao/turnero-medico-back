using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.DTOs.DoctorDTOs;

namespace turnero_medico_backend.DTOs.AuthDTOs
{
    // DTO para el registro de doctores.
    // Solo puede ser usado por un Admin (endpoint protegido con [Authorize(Roles = "Admin")]).
    // Hereda todas las validaciones de DoctorCreateDto y agrega únicamente Password.
    public class RegisterDoctorDto : DoctorCreateDto
    {
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}
