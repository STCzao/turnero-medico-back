using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    // Usado por Paciente, Doctor, Secretaria o Admin para cancelar un turno.
    // El motivo es opcional para el paciente, recomendado para secretaria/doctor.
    public class CancelarTurnoDto
    {
        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string? Motivo { get; set; }
    }
}
