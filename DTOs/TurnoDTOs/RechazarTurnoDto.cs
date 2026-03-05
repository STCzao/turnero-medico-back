using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    // Usado por Secretaria/Admin para rechazar una solicitud de turno.
    // El motivo es obligatorio para informar al paciente.
    public class RechazarTurnoDto
    {
        [Required(ErrorMessage = "El motivo de rechazo es obligatorio")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres")]
        public string MotivoRechazo { get; set; } = string.Empty;
    }
}
