namespace turnero_medico_backend.Models.Entities

{
    // Representa al profesional médico. La Matrícula es única y se usa como clave de vinculación:
    // si el Admin crea un Doctor vía CRUD y luego lo registra con cuenta, el registro existente
    // se vincula por Matrícula en lugar de duplicarse.
    public class Doctor
    {
        public int Id {get; set;}

        public string Matricula {get; set;} = string.Empty;  // Único — clave de vinculación con cuenta

        public string Nombre {get; set;} = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public int? EspecialidadId { get; set; }

        public Especialidad? Especialidad { get; set; }
        
        public string Dni { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        
        public string Telefono { get; set; } = string.Empty;

        // FK → AspNetUsers.Id. Une Doctor con su cuenta de usuario.
        public string? UserId { get; set; }

        // ===== Borrado lógico =====
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        
        // Relación: Un doctor puede tener muchos turnos
        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

        // Relación: Horarios de atención configurados
        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
    }
}