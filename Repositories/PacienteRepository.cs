using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    // Repositorio especializado para Paciente.
    // El Global Query Filter en ApplicationDbContext ya excluye IsDeleted = true en todas las queries.
    public class PacienteRepository(ApplicationDbContext context) : Repository<Paciente>(context), IPacienteRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        public async Task<(IEnumerable<Paciente> Items, int Total)> GetDependientesPagedAsync(
            string responsableId, int page, int pageSize)
        {
            var query = _ctx.Pacientes
                .Where(p => p.ResponsableId == responsableId);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
