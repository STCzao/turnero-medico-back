using AutoMapper;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class DoctorService(
        IDoctorRepository _repository,
        IRepository<Especialidad> _especialidadRepository,
        IMapper _mapper,
        ICurrentUserService _currentUserService
    ) : IDoctorService
    {
        public async Task<PagedResultDto<DoctorReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
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
            var createdWithNav = await _repository.GetByIdWithEspecialidadAsync(created.Id);
            return _mapper.Map<DoctorReadDto>(createdWithNav!);
        }

        public async Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto)
        {
            var doctor = await _repository.GetByIdWithEspecialidadAsync(id);
            if (doctor == null) return null;

            _ = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            _mapper.Map(dto, doctor);
            await _repository.UpdateAsync(doctor);
            var updatedWithNav = await _repository.GetByIdWithEspecialidadAsync(id);
            return _mapper.Map<DoctorReadDto>(updatedWithNav!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> ExistAsync(int id)
        {
            var doctor = await _repository.GetByIdAsync(id);
            return doctor != null;
        }
    }
}
