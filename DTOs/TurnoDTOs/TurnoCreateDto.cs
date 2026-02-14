using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    public class TurnoCreateDto
    {
        [Required(ErrorMessage = "El ID del paciente es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del paciente debe ser un número válido")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "La especialidad debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "La especialidad solo puede contener letras y espacios")]
        public string Especialidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID del doctor es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del doctor debe ser un número válido")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "La fecha y hora del turno es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; }

        [Required(ErrorMessage = "El motivo de la consulta es obligatorio")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres")]
        public string Motivo { get; set; } = string.Empty;
    }
}