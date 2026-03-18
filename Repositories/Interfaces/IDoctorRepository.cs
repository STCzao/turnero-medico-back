using System.Linq.Expressions;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<Doctor?> GetByIdWithEspecialidadAsync(int id);
        Task<IEnumerable<Doctor>> GetAllWithEspecialidadAsync();
        Task<(IEnumerable<Doctor> Items, int Total)> GetAllWithEspecialidadPagedAsync(int page, int pageSize);
        Task<IEnumerable<Doctor>> FindWithEspecialidadAsync(Expression<Func<Doctor, bool>> predicate);
    }
}
