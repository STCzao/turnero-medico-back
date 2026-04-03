using AutoMapper;
using Microsoft.AspNetCore.Identity;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    // Servicio de gestión de doctores.
    // El CRUD completo es exclusivo de Admin. Los doctores autenticados pueden consultar
    // su propio perfil a través de GetMyProfileAsync() sin conocer su ID numérico.
    // Al crear o actualizar, se recarga el doctor con Include(Especialidad) para que
    // el DTO devuelto tenga EspecialidadNombre completo.
    public class DoctorService(
        IDoctorRepository repository,
        IRepository<Especialidad> especialidadRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager
    ) : IDoctorService
    {
        private readonly IDoctorRepository _repository = repository;
        private readonly IRepository<Especialidad> _especialidadRepository = especialidadRepository;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IAuditService _auditService = auditService;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
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
            var doctor = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Doctor con ID {id} no encontrado.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                doctor.IsDeleted = true;
                doctor.DeletedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(doctor);

                if (!string.IsNullOrEmpty(doctor.UserId))
                {
                    var user = await _userManager.FindByIdAsync(doctor.UserId);
                    if (user != null)
                    {
                        await _userManager.SetLockoutEnabledAsync(user, true);
                        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _auditService.LogAsync(AuditAccion.Eliminar, "Doctor", id.ToString());
            return true;
        }

        public async Task<DoctorReadDto> ReactivarAsync(int id)
        {
            var doctor = await _repository.GetByIdUnscopedAsync(id)
                ?? throw new KeyNotFoundException($"Doctor con ID {id} no encontrado.");

            if (!doctor.IsDeleted)
                throw new InvalidOperationException("El doctor ya se encuentra activo.");

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                doctor.IsDeleted = false;
                doctor.DeletedAt = null;
                await _repository.UpdateAsync(doctor);

                if (!string.IsNullOrEmpty(doctor.UserId))
                {
                    var user = await _userManager.FindByIdAsync(doctor.UserId);
                    if (user != null)
                        await _userManager.SetLockoutEndDateAsync(user, null);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _auditService.LogAsync(AuditAccion.Actualizar, "Doctor", id.ToString());
            var reactivado = await _repository.GetByIdWithEspecialidadAsync(id);
            return _mapper.Map<DoctorReadDto>(reactivado);
        }

        public async Task<bool> ExistAsync(int id)
            => await _repository.ExistAsync(id);
    }
}
