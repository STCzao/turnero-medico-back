namespace turnero_medico_backend.Models.Entities
{
    public class Horario
    {
        public int Id { get; set; }

        // FK → Doctor
        public int DoctorId { get; set; }

        // 0=Domingo, 1=Lunes, 2=Martes, 3=Miercoles, 4=Jueves, 5=Viernes, 6=Sabado
        public int DiaSemana { get; set; }

        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }

        // Duración de cada turno en minutos (ej: 20, 30, 40)
        public int DuracionMinutos { get; set; } = 30;

        // Navegación
        public virtual Doctor Doctor { get; set; } = null!;
    }
}
