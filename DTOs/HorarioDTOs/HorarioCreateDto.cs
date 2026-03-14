using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.HorarioDTOs
{
    public class HorarioCreateDto
    {
        [Required(ErrorMessage = "El ID del doctor es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del doctor debe ser un número válido")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "El día de la semana es obligatorio")]
        [Range(0, 6, ErrorMessage = "DiaSemana debe ser entre 0 (Domingo) y 6 (Sábado)")]
        public int DiaSemana { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public TimeOnly HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        public TimeOnly HoraFin { get; set; }

        [Range(10, 120, ErrorMessage = "La duración debe ser entre 10 y 120 minutos")]
        public int DuracionMinutos { get; set; } = 30;
    }
}
