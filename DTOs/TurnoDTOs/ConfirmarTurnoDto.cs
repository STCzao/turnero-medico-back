using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    // Usado por Secretaria/Admin para confirmar una solicitud de turno.
    // La secretaria asigna fecha/hora y puede informar condiciones al paciente.
    public class ConfirmarTurnoDto
    {
        [Required(ErrorMessage = "La fecha y hora del turno es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; }

        // Doctor asignado. Obligatorio si el paciente no eligió uno al solicitar.
        [Range(1, int.MaxValue, ErrorMessage = "El ID del doctor debe ser un número válido")]
        public int? DoctorId { get; set; }

        // Condiciones informadas al paciente: copago, requisitos, documentación, etc.
        [StringLength(1000, ErrorMessage = "Las notas no pueden exceder 1000 caracteres")]
        public string? NotasSecretaria { get; set; }
    }
}
