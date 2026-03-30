using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    // Repositorio especializado para Doctor.
    // Todos los métodos With* cargan Especialidad via Include() para evitar N+1 queries
    // al mapear DoctorReadDto que necesita EspecialidadNombre.
    public class DoctorRepository(ApplicationDbContext context) : Repository<Doctor>(context), IDoctorRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        public async Task<Doctor?> GetByIdWithEspecialidadAsync(int id)
        {
            return await _ctx.Doctores
                .Include(d => d.Especialidad)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Doctor>> GetAllWithEspecialidadAsync()
        {
            return await _ctx.Doctores
                .Include(d => d.Especialidad)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Doctor> Items, int Total)> GetAllWithEspecialidadPagedAsync(int page, int pageSize)
        {
            var query = _ctx.Doctores.Include(d => d.Especialidad);
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<IEnumerable<Doctor>> FindWithEspecialidadAsync(Expression<Func<Doctor, bool>> predicate)
        {
            return await _ctx.Doctores
                .Include(d => d.Especialidad)
                .Where(predicate)
                .ToListAsync();
        }
    }
}
