namespace turnero_medico_backend.Models.Entities
{
    // Representa al personal de secretaría. Sigue el mismo patrón que Doctor:
    // el Admin crea el registro vía CRUD y luego registra la cuenta.
    // La vinculación con la cuenta de usuario se hace por DNI al registrarse.
    public class Secretaria
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        // DNI único — clave de vinculación con cuenta de usuario al registrarse
        public string Dni { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        // FK → AspNetUsers.Id. Null hasta que el Admin registre la cuenta.
        public string? UserId { get; set; }

        // ===== Borrado lógico =====
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
