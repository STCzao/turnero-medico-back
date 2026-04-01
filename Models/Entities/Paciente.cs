namespace turnero_medico_backend.Models.Entities
{
    // Representa al paciente del consultorio.
    // Un paciente puede ser autónomo (UserId != null, mayor de 18) o dependiente
    // (UserId == null, ResponsableId → cuenta del adulto responsable).
    // El DNI es la clave de vinculación: si la secretaria crea el Paciente antes de que
    // se registre la cuenta, el auto-registro lo detecta por DNI y vincula el UserId.
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

        // Plan dentro de la OS declarado por el paciente (ej: "Plan 310"). Nullable.
        public string? PlanAfiliado { get; set; }

        // FK → AspNetUsers.Id. Une Paciente con su cuenta de usuario (null si es dependiente sin cuenta).
        public string? UserId { get; set; }

        // ===== Borrado lógico =====
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // ===== Relaciones =====
        public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();
        
        public virtual ObraSocial? ObraSocial { get; set; }
    }
    
    // ===== ENUM: Tipos de Pago =====
    public enum TipoPago
    {
        ObraSocial,   // 0: tiene obra social
        Particular,   // 1: sin OS, paga de su bolsillo
    }
}
