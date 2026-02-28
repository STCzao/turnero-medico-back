namespace turnero_medico_backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(string email, string password, string nombre, string apellido, string rol);
        Task<(bool Success, string Message)> RegisterPacienteAsync(string email, string password, string nombre, string apellido, string dni, string telefono, DateTime fechaNacimiento);
        Task<(bool Success, string Message)> RegisterDoctorAsync(string email, string password, string nombre, string apellido, string matricula, string especialidad, string telefono);
        Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password);
    }
}
