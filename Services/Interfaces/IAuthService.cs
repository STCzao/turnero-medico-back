namespace turnero_medico_backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterPacienteAsync(string email, string password, string nombre, string apellido, string dni, string telefono, DateTime fechaNacimiento);
        Task<(bool Success, string Message)> RegisterDoctorAsync(string email, string password, string nombre, string apellido, string matricula, int especialidadId, string telefono, string? dni = null);
        Task<(bool Success, string Message)> RegisterSecretariaAsync(string email, string password, string nombre, string apellido, string? dni = null);
        Task<(bool Success, string Token, string RefreshToken, string Message)> LoginAsync(string email, string password);
        Task<(bool Success, string Token, string RefreshToken, string Message)> RefreshTokenAsync(string userId, string refreshToken);
    }
}
