using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    // Repositorio especializado para Paciente.
    // Todos los métodos excluyen registros con IsDeleted = true (borrado lógico).
    public class PacienteRepository(ApplicationDbContext context) : Repository<Paciente>(context), IPacienteRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        public async Task<(IEnumerable<Paciente> Items, int Total)> GetDependientesPagedAsync(
            string responsableId, int page, int pageSize)
        {
            var query = _ctx.Pacientes
                .Where(p => p.ResponsableId == responsableId && !p.IsDeleted);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        // Sobreescribimos los métodos del repositorio genérico para excluir registros eliminados lógicamente.

        public override async Task<Paciente?> GetByIdAsync(int id)
            => await _ctx.Pacientes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        public override async Task<IEnumerable<Paciente>> FindAsync(Expression<Func<Paciente, bool>> predicate)
            => await _ctx.Pacientes.Where(p => !p.IsDeleted).Where(predicate).ToListAsync();

        public override async Task<bool> ExistAsync(int id)
            => await _ctx.Pacientes.AnyAsync(p => p.Id == id && !p.IsDeleted);

        public override async Task<(IEnumerable<Paciente> Items, int Total)> GetAllPagedAsync(int page, int pageSize)
        {
            var query = _ctx.Pacientes.Where(p => !p.IsDeleted);
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }
    }
}
