using AutoMapper;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class DoctorService(
        IDoctorRepository repository,
        IRepository<Especialidad> especialidadRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ApplicationDbContext dbContext
    ) : IDoctorService
    {
        private readonly IDoctorRepository _repository = repository;
        private readonly IRepository<Especialidad> _especialidadRepository = especialidadRepository;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IAuditService _auditService = auditService;
        private readonly ApplicationDbContext _dbContext = dbContext;
        public async Task<PagedResultDto<DoctorReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = await _repository.GetAllWithEspecialidadPagedAsync(page, pageSize);
            return new PagedResultDto<DoctorReadDto>
            {
                Items = _mapper.Map<IEnumerable<DoctorReadDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<DoctorReadDto>> GetByEspecialidadAsync(int especialidadId)
        {
            var doctors = await _repository.FindWithEspecialidadAsync(d => d.EspecialidadId == especialidadId);
            return _mapper.Map<IEnumerable<DoctorReadDto>>(doctors);
        }

        public async Task<DoctorReadDto?> GetByIdAsync(int id)
        {
            var doctor = await _repository.GetByIdWithEspecialidadAsync(id);
            if (doctor == null) return null;
            return _mapper.Map<DoctorReadDto>(doctor);
        }

        public async Task<DoctorReadDto?> GetMyProfileAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado");

            var doctors = await _repository.FindWithEspecialidadAsync(d => d.UserId == userId);
            var doctor = doctors.FirstOrDefault();
            if (doctor == null) return null;
            return _mapper.Map<DoctorReadDto>(doctor);
        }

        public async Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto)
        {
            _ = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            var doctor = _mapper.Map<Doctor>(dto);
            var created = await _repository.AddAsync(doctor);
            await _auditService.LogAsync(AuditAccion.Crear, "Doctor", created.Id.ToString());
            var createdWithNav = await _repository.GetByIdWithEspecialidadAsync(created.Id);
            return _mapper.Map<DoctorReadDto>(createdWithNav!);
        }

        public async Task<DoctorReadDto> UpdateAsync(int id, DoctorUpdateDto dto)
        {
            var doctor = await _repository.GetByIdWithEspecialidadAsync(id)
                ?? throw new KeyNotFoundException($"Doctor con ID {id} no encontrado.");

            _ = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            _mapper.Map(dto, doctor);
            await _repository.UpdateAsync(doctor);
            await _auditService.LogAsync(AuditAccion.Actualizar, "Doctor", id.ToString());
            var updatedWithNav = await _repository.GetByIdWithEspecialidadAsync(id);
            return _mapper.Map<DoctorReadDto>(updatedWithNav!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await _repository.ExistAsync(id))
                throw new KeyNotFoundException($"Doctor con ID {id} no encontrado.");

            var tieneTurnos = await _dbContext.Turnos.AnyAsync(t => t.DoctorId == id);
            if (tieneTurnos)
                throw new InvalidOperationException(
                    "No se puede eliminar el doctor porque tiene turnos asociados. Cancele o reasigne los turnos primero.");

            var deleted = await _repository.DeleteAsync(id);
            if (deleted)
                await _auditService.LogAsync(AuditAccion.Eliminar, "Doctor", id.ToString());
            return deleted;
        }

        public async Task<bool> ExistAsync(int id)
            => await _repository.ExistAsync(id);
    }
}
