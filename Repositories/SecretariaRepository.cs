using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    public class SecretariaRepository(ApplicationDbContext context) : Repository<Secretaria>(context), ISecretariaRepository
    {
        private readonly ApplicationDbContext _ctx = context;

        // Sobreescribe el paginado genérico para ordenar por Apellido en lugar de Id
        public override async Task<(IEnumerable<Secretaria> Items, int Total)> GetAllPagedAsync(int page, int pageSize)
        {
            var query = _ctx.Secretarias.AsQueryable();
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }
    }
}
