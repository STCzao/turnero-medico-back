using System.Linq.Expressions;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    // Repositorio especializado para Doctor.
    // Los métodos WithEspecialidad cargan la relación Especialidad para construir
    // DoctorReadDto con EspecialidadNombre sin queries adicionales (evita N+1).
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<Doctor?> GetByIdWithEspecialidadAsync(int id);
        Task<IEnumerable<Doctor>> GetAllWithEspecialidadAsync();
        Task<(IEnumerable<Doctor> Items, int Total)> GetAllWithEspecialidadPagedAsync(int page, int pageSize);
        Task<IEnumerable<Doctor>> FindWithEspecialidadAsync(Expression<Func<Doctor, bool>> predicate);
    }
}
