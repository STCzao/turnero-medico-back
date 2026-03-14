using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    public class PacienteRepository(ApplicationDbContext context) : Repository<Paciente>(context), IPacienteRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        public async Task<Paciente?> GetByIdWithObraSocialAsync(int id)
        {
            return await _ctx.Pacientes
                .Include(p => p.ObraSocial)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Paciente>> GetAllWithObraSocialAsync()
        {
            return await _ctx.Pacientes
                .Include(p => p.ObraSocial)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Paciente> Items, int Total)> GetAllWithObraSocialPagedAsync(int page, int pageSize)
        {
            var query = _ctx.Pacientes.Include(p => p.ObraSocial);
            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<IEnumerable<Paciente>> FindWithObraSocialAsync(Expression<Func<Paciente, bool>> predicate)
        {
            return await _ctx.Pacientes
                .Include(p => p.ObraSocial)
                .Where(predicate)
                .ToListAsync();
        }
    }
}
