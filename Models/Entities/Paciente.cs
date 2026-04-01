namespace turnero_medico_backend.Models.Entities
{
    // Representa al paciente del consultorio.
    // Un paciente puede ser autónomo (UserId != null, mayor de 18) o dependiente
    // (UserId == null, ResponsableId → cuenta del adulto responsable).
    // El DNI es la clave de vinculación: si la secretaria crea el Paciente antes de que
    // se registre la cuenta, el auto-registro lo detecta por DNI y vincula el UserId.
    // La cobertura médica (OS, número de afiliado, plan) se declara por turno, no en el perfil.
    public class Paciente
    {
        public int Id { get; set; }

        public string Dni { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;  // ← nullable para dependientes sin AspNetUser

        public string Telefono { get; set; } = string.Empty;

        public DateTime FechaNacimiento { get; set; }

        // ===== Relación Familiar =====
        public string? ResponsableId { get; set; }  // FK → AspNetUser (nullable)

        public bool EsMayorDeEdad { get; set; }  // Para control de lógica

        // FK → AspNetUsers.Id. Une Paciente con su cuenta de usuario (null si es dependiente sin cuenta).
        public string? UserId { get; set; }

        // ===== Borrado lógico =====
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // ===== Relaciones =====
        public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    }
}
