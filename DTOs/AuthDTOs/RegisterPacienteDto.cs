using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.DTOs.PacienteDTOs;

namespace turnero_medico_backend.DTOs.AuthDTOs
{
    // DTO para el auto-registro público de pacientes.
    // Hereda todas las validaciones de PacienteCreateDto y agrega únicamente Password.
    public class RegisterPacienteDto : PacienteCreateDto
    {
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres")]
        public string Password { get; set; } = string.Empty;
    }
}
