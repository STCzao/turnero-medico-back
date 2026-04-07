using System.Linq.Expressions;

namespace turnero_medico_backend.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // GET
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<(IEnumerable<T> Items, int Total)> GetAllPagedAsync(int page, int pageSize);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistAsync(int id);

        // POST
        Task<T> AddAsync(T entity);

        // PUT
        Task<T> UpdateAsync(T entity);

        // DELETE
        Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteAsync(int id);

        // UNSCOPED (ignora Global Query Filters — solo para reactivación y vinculación de cuentas)
        Task<T?> GetByIdUnscopedAsync(int id);

        // SAVE
        Task<bool> SaveChangesAsync();
    }
}
