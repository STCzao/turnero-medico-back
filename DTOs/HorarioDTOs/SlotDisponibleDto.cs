namespace turnero_medico_backend.DTOs.HorarioDTOs
{
    // Slot de tiempo disponible para agendar turno
    public class SlotDisponibleDto
    {
        public DateTime FechaHora { get; set; }
        public int DoctorId { get; set; }
        public string DoctorNombre { get; set; } = string.Empty;
        public int DuracionMinutos { get; set; }
    }
}
