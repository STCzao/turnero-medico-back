using AutoMapper;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class DoctorService(
        IRepository<Doctor> _repository,
        IRepository<Especialidad> _especialidadRepository,
        IMapper _mapper,
        ICurrentUserService _currentUserService
    ) : IDoctorService
    {
        private async Task<string> GetEspecialidadNombreAsync(int especialidadId)
        {
            var esp = await _especialidadRepository.GetByIdAsync(especialidadId);
            return esp?.Nombre ?? string.Empty;
        }

        private async Task<Dictionary<int, string>> BuildEspecialidadMapAsync(IEnumerable<Doctor> doctors)
        {
            var ids = doctors.Select(d => d.EspecialidadId).Distinct().ToList();
            var especialidades = await _especialidadRepository.FindAsync(e => ids.Contains(e.Id));
            return especialidades.ToDictionary(e => e.Id, e => e.Nombre);
        }

        public async Task<IEnumerable<DoctorReadDto>> GetAllAsync()
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado de doctores.");

            var doctors = await _repository.GetAllAsync();
            var espMap = await BuildEspecialidadMapAsync(doctors);
            return doctors.Select(d =>
            {
                var dto = _mapper.Map<DoctorReadDto>(d);
                dto.EspecialidadNombre = espMap.TryGetValue(d.EspecialidadId, out var nombre) ? nombre : string.Empty;
                return dto;
            });
        }

        public async Task<PagedResultDto<DoctorReadDto>> GetAllPagedAsync(int page, int pageSize)
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado de doctores.");

            var (items, total) = await _repository.GetAllPagedAsync(page, pageSize);
            var espMap = await BuildEspecialidadMapAsync(items);
            return new PagedResultDto<DoctorReadDto>
            {
                Items = items.Select(d =>
                {
                    var dto = _mapper.Map<DoctorReadDto>(d);
                    dto.EspecialidadNombre = espMap.TryGetValue(d.EspecialidadId, out var nombre) ? nombre : string.Empty;
                    return dto;
                }),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<DoctorReadDto>> GetByEspecialidadAsync(int especialidadId)
        {
            var doctors = await _repository.FindAsync(d => d.EspecialidadId == especialidadId);
            var espNombre = await GetEspecialidadNombreAsync(especialidadId);
            return doctors.Select(d =>
            {
                var dto = _mapper.Map<DoctorReadDto>(d);
                dto.EspecialidadNombre = espNombre;
                return dto;
            });
        }

        public async Task<DoctorReadDto?> GetByIdAsync(int id)
        {
            var doctor = await _repository.GetByIdAsync(id);
            if (doctor == null) return null;
            var dto = _mapper.Map<DoctorReadDto>(doctor);
            dto.EspecialidadNombre = await GetEspecialidadNombreAsync(doctor.EspecialidadId);
            return dto;
        }

        public async Task<DoctorReadDto?> GetMyProfileAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario autenticado");

            var doctores = await _repository.FindAsync(d => d.UserId == userId);
            var doctor = doctores.FirstOrDefault();

            if (doctor == null)
                return null;

            var dto = _mapper.Map<DoctorReadDto>(doctor);
            dto.EspecialidadNombre = await GetEspecialidadNombreAsync(doctor.EspecialidadId);
            return dto;
        }

        public async Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto)
        {
            var especialidad = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            var doctor = _mapper.Map<Doctor>(dto);
            var createdDoctor = await _repository.AddAsync(doctor);
            var result = _mapper.Map<DoctorReadDto>(createdDoctor);
            result.EspecialidadNombre = especialidad.Nombre;
            return result;
        }

        public async Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto)
        {
            var doctor = await _repository.GetByIdAsync(id);
            if (doctor == null) return null;

            var especialidad = await _especialidadRepository.GetByIdAsync(dto.EspecialidadId)
                ?? throw new InvalidOperationException($"La especialidad con ID {dto.EspecialidadId} no existe.");

            _mapper.Map(dto, doctor);
            await _repository.UpdateAsync(doctor);
            var result = _mapper.Map<DoctorReadDto>(doctor);
            result.EspecialidadNombre = especialidad.Nombre;
            return result;
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
