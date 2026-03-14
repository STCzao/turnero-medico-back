using System.Linq.Expressions;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    public interface IPacienteRepository : IRepository<Paciente>
    {
        Task<Paciente?> GetByIdWithObraSocialAsync(int id);
        Task<IEnumerable<Paciente>> GetAllWithObraSocialAsync();
        Task<(IEnumerable<Paciente> Items, int Total)> GetAllWithObraSocialPagedAsync(int page, int pageSize);
        Task<IEnumerable<Paciente>> FindWithObraSocialAsync(Expression<Func<Paciente, bool>> predicate);
    }
}
