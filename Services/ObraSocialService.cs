using AutoMapper;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class ObraSocialService(
        IRepository<ObraSocial> obraSocialRepository,
        IMapper mapper,
        CurrentUserService currentUserService) : IObraSocialService
    {
        private readonly IRepository<ObraSocial> _obraSocialRepository = obraSocialRepository;
        private readonly IMapper _mapper = mapper;
        private readonly CurrentUserService _currentUserService = currentUserService;

        /// <summary>
        /// Obtiene todas las obras sociales (solo Admin)
        /// </summary>
        public async Task<IEnumerable<ObraSocialReadDto>> GetAllAsync()
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver las obras sociales. Solo Admin.");

            var obras = await _obraSocialRepository.GetAllAsync();
            return obras.Select(o => _mapper.Map<ObraSocialReadDto>(o));
        }

        /// <summary>
        /// Obtiene una obra social por ID (solo Admin)
        /// </summary>
        public async Task<ObraSocialReadDto?> GetByIdAsync(int id)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver las obras sociales. Solo Admin.");

            var obra = await _obraSocialRepository.GetByIdAsync(id);
            return obra != null ? _mapper.Map<ObraSocialReadDto>(obra) : null;
        }

        /// <summary>
        /// Crea una nueva obra social (solo Admin)
        /// </summary>
        public async Task<ObraSocialReadDto> CreateAsync(ObraSocialCreateDto dto)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para crear obras sociales. Solo Admin.");

            // Validar que no exista una obra social con el mismo nombre
            var existente = await _obraSocialRepository.FindAsync(o => o.Nombre == dto.Nombre);
            if (existente.Any())
                throw new InvalidOperationException($"Ya existe una obra social con el nombre '{dto.Nombre}'");

            var obra = _mapper.Map<ObraSocial>(dto);
            var createdObra = await _obraSocialRepository.AddAsync(obra);
            return _mapper.Map<ObraSocialReadDto>(createdObra);
        }

        /// <summary>
        /// Elimina una obra social (solo Admin)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para eliminar obras sociales. Solo Admin.");

            var obra = await _obraSocialRepository.GetByIdAsync(id);
            if (obra == null)
                return false;

            // Validar que no haya pacientes asociados (opcional, pero recomendado)
            // Por seguridad, podrías agregar esta validación en el futuro

            return await _obraSocialRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Verifica si una obra social existe
        /// </summary>
        public async Task<bool> ExistAsync(int id)
        {
            var obra = await _obraSocialRepository.GetByIdAsync(id);
            return obra != null;
        }
    }
}
