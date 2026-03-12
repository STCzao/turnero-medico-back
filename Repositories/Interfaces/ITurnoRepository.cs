using System.Linq.Expressions;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Repositories.Interfaces
{
    // Repositorio especializado para Turno.
    // Extiende IRepository<Turno> con métodos que cargan las propiedades de navegación
    // (Paciente, Doctor, ObraSocial) necesarias para construir TurnoReadDto correctamente.
    // Las operaciones de escritura siguen usando los métodos base (no necesitan navigation props).
    public interface ITurnoRepository : IRepository<Turno>
    {
        // Trae un turno por ID con Paciente, Doctor y ObraSocial cargados.
        Task<Turno?> GetByIdWithDetailsAsync(int id);

        // Filtra turnos con Paciente, Doctor y ObraSocial cargados.
        Task<IEnumerable<Turno>> FindWithDetailsAsync(Expression<Func<Turno, bool>> predicate);

        // Paginación con filtro opcional de estado. Todo se evalúa en base de datos.
        // Soluciona además el bug del análisis anterior donde el filtrado + paginación
        // se hacía en memoria en lugar de en el servidor de base de datos.
        Task<(IEnumerable<Turno> Items, int Total)> GetAllWithDetailsPagedAsync(int page, int pageSize, string? estado = null);
    }
}
