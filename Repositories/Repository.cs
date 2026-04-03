using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.Repositories.Interfaces;

namespace turnero_medico_backend.Repositories
{
    // Implementación genérica del patrón Repository.
    // Proporciona CRUD estándar para cualquier entidad. Los repositorios especializados
    // (TurnoRepository, PacienteRepository, DoctorRepository) heredan de esta clase y
    // agregan métodos con Include() para cargar propiedades de navegación cuando sean necesarias.
    public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context = context;
        private readonly DbSet<T> _dbSet = context.Set<T>();

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // Paginación con orden por Id. EF.Property permite ordernar por nombre de columna
        // sin que T necesite implementar ninguna interfaz específica.
        public virtual async Task<(IEnumerable<T> Items, int Total)> GetAllPagedAsync(int page, int pageSize)
        {
            var total = await _dbSet.CountAsync();
            var items = await _dbSet
                .OrderBy(e => EF.Property<int>(e, "Id"))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // Usa AnyAsync en lugar de FindAsync + null-check para evitar cargar la entidad completa
        public virtual async Task<bool> ExistAsync(int id)
            => await _dbSet.AnyAsync(e => EF.Property<int>(e, "Id") == id);

        public async Task<T> AddAsync(T entity)
        {
            _dbSet.Add(entity);
            await SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _context.Entry(entity).Property("IsDeleted").CurrentValue = true;
            _context.Entry(entity).Property("DeletedAt").CurrentValue = DateTime.UtcNow;
            await SaveChangesAsync();
            return true;
        }

        public async Task<T?> GetByIdUnscopedAsync(int id)
        {
            return await _dbSet.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
