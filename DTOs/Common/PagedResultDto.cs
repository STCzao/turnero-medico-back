namespace turnero_medico_backend.DTOs.Common
{
    // Envuelve un listado paginado de elementos junto con metadatos de paginación.
    // Usado en todos los endpoints de listado (GET /api/Turnos, GET /api/Doctores, etc.)
    public class PagedResultDto<T>
    {
        // Elementos de la página actual.
        public IEnumerable<T> Items { get; set; } = [];

        // Total de registros en la base de datos (sin paginar).
        public int Total { get; set; }

        // Número de la página actual (base 1).
        public int Page { get; set; }

        // Cantidad de elementos por página.
        public int PageSize { get; set; }

        // Total de páginas calculado.
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

        // Indica si existe una página anterior.
        public bool HasPreviousPage => Page > 1;

        // Indica si existe una página siguiente.
        public bool HasNextPage => Page < TotalPages;
    }
}
