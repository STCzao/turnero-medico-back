using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    public class TurnoUpdateDto
    {
        [Required(ErrorMessage = "El ID es obligatorio")]
        public int Id { get; set; }

        [StringLength(30, MinimumLength = 3, ErrorMessage = "La especialidad debe tener entre {2} y {1} caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "La especialidad solo puede contener letras y espacios")]
        public string? Especialidad { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El ID del doctor debe ser un número válido")]
        public int? DoctorId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? FechaHora { get; set; }

        [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres")]
        public string? Motivo { get; set; }

        [RegularExpression(
            @"^(Pendiente|Confirmado|Cancelado|Completado|Aceptado|Rechazado|PendienteValidacionDoctor)$",
            ErrorMessage = "Estado inválido. Valores permitidos: Pendiente, Confirmado, Cancelado, Completado, Aceptado, Rechazado, PendienteValidacionDoctor")]
        public string? Estado { get; set; }
    }
}
