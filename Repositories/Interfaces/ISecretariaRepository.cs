using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    // IRepository<T> ya expone GetAllPagedAsync, FindAsync, etc.
    // Esta interfaz existe para que el DI container lo resuelva como tipo específico.
    public interface ISecretariaRepository : IRepository<Secretaria>
    {
    }
}
