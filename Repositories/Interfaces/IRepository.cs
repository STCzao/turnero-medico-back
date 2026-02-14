namespace turnero_medico_backend.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // GET
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);

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
