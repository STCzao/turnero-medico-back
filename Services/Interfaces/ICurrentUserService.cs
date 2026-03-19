namespace turnero_medico_backend.Services.Interfaces
{
    public interface ICurrentUserService
    {
        string? GetUserId();
        string? GetUserRole();
        bool IsAdmin();
        string? GetUserEmail();
        string? GetUserName();
    }
}
