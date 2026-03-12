namespace turnero_medico_backend.DTOs.HorarioDTOs
{
    public class HorarioReadDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorNombre { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public string DiaSemanaTexto { get; set; } = string.Empty;
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
    }
}
