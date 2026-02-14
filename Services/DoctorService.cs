using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services
{
    public class DoctorService(IRepository<Doctor> _repository) : IDoctorService
    {

        public async Task<IEnumerable<DoctorReadDto>> GetAllAsync()
        {
            var doctors = await _repository.GetAllAsync();
            return doctors.Select(d => Mapper.MapToDoctorReadDto(d));
        }

        public async Task<IEnumerable<DoctorReadDto>> GetByEspecialidadAsync(string especialidad)
        {
            var doctors = await _repository.FindAsync(d => 
                d.Especialidad.ToLower() == especialidad.ToLower());
            return doctors.Select(d => Mapper.MapToDoctorReadDto(d));
        }

        public async Task<DoctorReadDto?> GetByIdAsync(int id)
        {
            var doctor = await _repository.GetByIdAsync(id);
            return doctor == null ? null : Mapper.MapToDoctorReadDto(doctor);
        }

        public async Task<DoctorReadDto> CreateAsync(DoctorCreateDto dto)
        {
            var doctor = Mapper.MapToDoctor(dto);
            var createdDoctor = await _repository.AddAsync(doctor);
            return Mapper.MapToDoctorReadDto(createdDoctor);
        }

        public async Task<DoctorReadDto?> UpdateAsync(int id, DoctorUpdateDto dto)
        {
            var doctor = await _repository.GetByIdAsync(id);
            if (doctor == null)
                return null;

            var updatedDoctor = Mapper.MapToDoctor(dto, doctor);
            await _repository.UpdateAsync(updatedDoctor);
            return Mapper.MapToDoctorReadDto(updatedDoctor);
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
