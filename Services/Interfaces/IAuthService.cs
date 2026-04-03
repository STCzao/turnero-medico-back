namespace turnero_medico_backend.Services.Interfaces
{
    // Contrato de autenticación y registro de usuarios.
    // Los métodos de registro lanzan excepciones para errores de negocio (email duplicado,
    // matrícula en uso, etc.) — capturadas por GlobalExceptionMiddleware.
    // Login y RefreshToken conservan el patrón de tupla ya que necesitan retornar tokens
    // y mapear explícitamente a 401 (no a 400/500).
    public interface IAuthService
    {
        // Auto-registro público. Vincula por DNI si el paciente ya fue creado por secretaria.
        // Lanza InvalidOperationException si el email/DNI ya están en uso.
        Task<string> RegisterPacienteAsync(string email, string password, string nombre, string apellido, string dni, string telefono, DateTime fechaNacimiento);

        // Solo Admin. Vincula por Matrícula si el doctor ya existe en el catálogo.
        // Lanza InvalidOperationException si el email/matrícula ya están en uso.
        Task<string> RegisterDoctorAsync(string email, string password, string nombre, string apellido, string matricula, int especialidadId, string telefono, string? dni = null);

        // Solo Admin. Vincula por DNI si la secretaria ya existe en el catálogo.
        // Lanza InvalidOperationException si el email/DNI ya están en uso.
        Task<string> RegisterSecretariaAsync(string email, string password, string nombre, string apellido, string dni, string telefono);

        // Devuelve access token (30 min) y refresh token (30 días). Ambos en texto plano.
        // El refresh token se almacena en DB como SHA-256, nunca en claro.
        Task<(bool Success, string Token, string RefreshToken, string Message)> LoginAsync(string email, string password);

        // Rota el par: invalida el refresh anterior y emite uno nuevo. Previene reutilización.
        Task<(bool Success, string Token, string RefreshToken, string Message)> RefreshTokenAsync(string userId, string refreshToken);
    }
}
