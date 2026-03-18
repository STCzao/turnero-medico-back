using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    public class TurnoRepository(ApplicationDbContext context) : Repository<Turno>(context), ITurnoRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        public async Task<Turno?> GetByIdWithDetailsAsync(int id)
        {
            return await _ctx.Turnos
                .AsNoTracking()
                .Include(t => t.Paciente)
                .Include(t => t.Doctor)
                .Include(t => t.ObraSocial)
                .Include(t => t.Especialidad)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Turno>> FindWithDetailsAsync(Expression<Func<Turno, bool>> predicate)
        {
            return await _ctx.Turnos
                .AsNoTracking()
                .Include(t => t.Paciente)
                .Include(t => t.Doctor)
                .Include(t => t.ObraSocial)
                .Include(t => t.Especialidad)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Turno> Items, int Total)> GetAllWithDetailsPagedAsync(int page, int pageSize, string? estado = null)
        {
            var query = _ctx.Turnos
                .AsNoTracking()
                .Include(t => t.Paciente)
                .Include(t => t.Doctor)
                .Include(t => t.ObraSocial)
                .Include(t => t.Especialidad)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(t => t.Estado == estado);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
