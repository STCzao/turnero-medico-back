namespace turnero_medico_backend.Models.Entities
{
    public class Paciente
    {
        public int Id { get; set; }
        
        public string Dni { get; set; } = string.Empty;
        
        public string Nombre { get; set; } = string.Empty;
        
        public string Apellido { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;  // ← nullable para dependientes sin AspNetUser
        
        public string Telefono { get; set; } = string.Empty;
        
        public DateTime FechaNacimiento { get; set; }
        
        // ===== NUEVO: Relación Familiar =====
        public string? ResponsableId { get; set; }  // FK → AspNetUser (nullable)
        
        public bool EsMayorDeEdad { get; set; }  // Para control de lógica
        
        // ===== NUEVO: Cobertura Médica =====
        public TipoPago TipoPago { get; set; } = TipoPago.ObraSocial;
        
        public int? ObraSocialId { get; set; }  // FK → ObraSocial (nullable)
        
        public string NumeroAfiliado { get; set; } = string.Empty;
        
        // ===== Relaciones =====
        public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();
        
        public virtual ObraSocial? ObraSocial { get; set; }
    }
    
    // ===== ENUM: Tipos de Pago =====
    public enum TipoPago
    {
        ObraSocial,
        Particular,
        SinCobertura
    }
}
