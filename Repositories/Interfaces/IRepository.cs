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

        // POST
        Task<T> AddAsync(T entity);

        // PUT
        Task<T> UpdateAsync(T entity);

        // DELETE
        Task<bool> DeleteAsync(int id);

        // SAVE
        Task<bool> SaveChangesAsync();
    }
}
