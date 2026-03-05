using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    // Usado exclusivamente por el Doctor para actualizar el estado clínico del turno.
    // La gestión administrativa (confirmar/rechazar/cancelar) tiene sus propios DTOs.
    public class TurnoUpdateDto
    {
        [Required(ErrorMessage = "El ID es obligatorio")]
        public int Id { get; set; }

        // Doctor puede marcar Completado o Ausente únicamente.
        [RegularExpression(
            @"^(Completado|Ausente)$",
            ErrorMessage = "Estado inválido. El doctor solo puede marcar 'Completado' o 'Ausente'")]
        public string? Estado { get; set; }

        // Observación clínica libre del doctor (diagnóstico, derivación, notas).
        [StringLength(1000, ErrorMessage = "La observación no puede exceder 1000 caracteres")]
        public string? ObservacionClinica { get; set; }
    }
}
