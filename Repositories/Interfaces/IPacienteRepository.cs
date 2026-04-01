using System.Linq.Expressions;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    // Repositorio especializado para Paciente.
    // Todos los métodos filtran IsDeleted = true (borrado lógico).
    public interface IPacienteRepository : IRepository<Paciente>
    {
        Task<(IEnumerable<Paciente> Items, int Total)> GetDependientesPagedAsync(
            string responsableId, int page, int pageSize);
    }
}
