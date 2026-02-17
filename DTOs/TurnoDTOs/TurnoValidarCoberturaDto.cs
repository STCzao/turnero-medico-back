using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.TurnoDTOs
{
    /// <summary>
    /// DTO para que el doctor valide la cobertura de un turno en caso especial
    /// </summary>
    public class TurnoValidarCoberturaDto
    {
        [Required(ErrorMessage = "El resultado de validación es obligatorio")]
        [RegularExpression("Aceptado|Rechazado", ErrorMessage = "El resultado debe ser 'Aceptado' o 'Rechazado'")]
        public string Resultado { get; set; } = string.Empty;  // "Aceptado" o "Rechazado"

        [StringLength(500, ErrorMessage = "El motivo de rechazo no puede exceder 500 caracteres")]
        public string? MotivoRechazo { get; set; }  // Obligatorio si Resultado = "Rechazado"
    }
}
