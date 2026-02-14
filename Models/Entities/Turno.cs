namespace turnero_medico_backend.Models.Entities
{
    public class Turno
    {
        public int Id { get; set; }
        
        public DateTime FechaHora { get; set; }
        
        public string Motivo { get; set; } = string.Empty;
        
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Confirmado, Cancelado, Completado
        
        // Claves foráneas
        public int PacienteId { get; set; }
        public int DoctorId { get; set; }
        
        // Propiedades de navegación
        public Paciente Paciente { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
    }
}