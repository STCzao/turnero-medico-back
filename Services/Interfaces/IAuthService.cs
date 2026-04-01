namespace turnero_medico_backend.Services.Interfaces
{
    // Contrato de autenticación y registro de usuarios.
    // Cada método de registro retorna (bool, string) para reportar errores sin lanzar excepciones,
    // ya que errores de registro son flujo normal (email duplicado, matrícula en uso, etc.).
    // Login y RefreshToken retornan los tokens directamente en la tupla.
    public interface IAuthService
    {
        // Auto-registro público. Vincula por DNI si el paciente ya fue creado por secretaria.
        Task<(bool Success, string Message)> RegisterPacienteAsync(string email, string password, string nombre, string apellido, string dni, string telefono, DateTime fechaNacimiento);

        // Solo Admin. Vincula por Matrícula si el doctor ya existe en el catálogo.
        Task<(bool Success, string Message)> RegisterDoctorAsync(string email, string password, string nombre, string apellido, string matricula, int especialidadId, string telefono, string? dni = null);

        // Solo Admin. Vincula por DNI si la secretaria ya existe en el catálogo.
        Task<(bool Success, string Message)> RegisterSecretariaAsync(string email, string password, string nombre, string apellido, string dni);

        // Devuelve access token (30 min) y refresh token (30 días). Ambos en texto plano.
        // El refresh token se almacena en DB como SHA-256, nunca en claro.
        Task<(bool Success, string Token, string RefreshToken, string Message)> LoginAsync(string email, string password);

        // Rota el par: invalida el refresh anterior y emite uno nuevo. Previene reutilización.
        Task<(bool Success, string Token, string RefreshToken, string Message)> RefreshTokenAsync(string userId, string refreshToken);
    }
}
