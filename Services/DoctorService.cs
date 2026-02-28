using AutoMapper;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class DoctorService(
        IRepository<Doctor> _repository,
        IMapper _mapper,
        CurrentUserService _currentUserService
    ) : IDoctorService
    {

        public async Task<IEnumerable<DoctorReadDto>> GetAllAsync()
        {
            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("No tienes permisos para ver el listado de doctores.");

            var doctors = await _repository.GetAllAsync();
            return doctors.Select(d => _mapper.Map<DoctorReadDto>(d));
        }

        public async Task<IEnumerable<DoctorReadDto>> GetByEspecialidadAsync(string especialidad)
        {
            var doctors = await _repository.FindAsync(d => 
                string.Equals(d.Especialidad, especialidad, StringComparison.OrdinalIgnoreCase));
            return doctors.Select(_mapper.Map<DoctorReadDto>);
        }

        public async Task<DoctorReadDto?> GetByIdAsync(int id)
        {
            var doctor = await _repository.GetByIdAsync(id);
            return doctor == null ? null : _mapper.Map<DoctorReadDto>(doctor);
        }

        /// 
        /// Obtiene el perfil del doctor autenticado actual
        /// </summary>
        public async Task<DoctorReadDto?> GetMyProfileAsync()
        {
            var userEmail = _currentUserService.GetUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedAccessException("No se pudo obtener el email del usuario autenticado");

            var doctores = await _repository.FindAsync(d => d.Email == userEmail);
            var doctor = doctores.FirstOrDefault();

            if (doctor == null)
                return null;

            return _mapper.Map<DoctorReadDto>(doctor);
        }

        public async Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto)
        {
            var doctor = _mapper.Map<Doctor>(dto);
            var createdDoctor = await _repository.AddAsync(doctor);
            return _mapper.Map<DoctorReadDto>(createdDoctor);
        }

        public async Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto)
        {
            var doctor = await _repository.GetByIdAsync(id);
            if (doctor == null)
                return null;

            var updatedDoctor = _mapper.Map(dto, doctor);
            await _repository.UpdateAsync(updatedDoctor);
            return _mapper.Map<DoctorReadDto>(updatedDoctor);
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
