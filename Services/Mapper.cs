using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Services
{
    public class Mapper
    {
        // PACIENTE MAPPINGS
        public static Paciente MapToPaciente(PacienteCreateDto dto)
        {
            return new Paciente
            {
                Dni = dto.Dni,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Telefono = dto.Telefono,
                FechaNacimiento = dto.FechaNacimiento
            };
        }

        public static Paciente MapToPaciente(PacienteUpdateDto dto, Paciente paciente)
        {
            paciente.Dni = dto.Dni;
            paciente.Nombre = dto.Nombre;
            paciente.Apellido = dto.Apellido;
            paciente.Email = dto.Email;
            paciente.Telefono = dto.Telefono;
            paciente.FechaNacimiento = dto.FechaNacimiento;
            return paciente;
        }

        public static PacienteReadDto MapToPacienteReadDto(Paciente paciente)
        {
            return new PacienteReadDto
            {
                Id = paciente.Id,
                Dni = paciente.Dni,
                Nombre = paciente.Nombre,
                Apellido = paciente.Apellido,
                Email = paciente.Email,
                Telefono = paciente.Telefono,
                FechaNacimiento = paciente.FechaNacimiento
            };
        }

        // DOCTOR MAPPINGS
        public static Doctor MapToDoctor(DoctorCreateDto dto)
        {
            return new Doctor
            {
                Matricula = dto.Matricula,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Especialidad = dto.Especialidad,
                Email = dto.Email,
                Telefono = dto.Telefono
            };
        }

        public static Doctor MapToDoctor(DoctorUpdateDto dto, Doctor doctor)
        {
            doctor.Matricula = dto.Matricula;
            doctor.Nombre = dto.Nombre;
            doctor.Apellido = dto.Apellido;
            doctor.Especialidad = dto.Especialidad;
            doctor.Email = dto.Email;
            doctor.Telefono = dto.Telefono;
            return doctor;
        }

        public static DoctorReadDto MapToDoctorReadDto(Doctor doctor)
        {
            return new DoctorReadDto
            {
                Id = doctor.Id,
                Matricula = doctor.Matricula,
                Nombre = doctor.Nombre,
                Apellido = doctor.Apellido,
                Especialidad = doctor.Especialidad,
                Email = doctor.Email,
                Telefono = doctor.Telefono
            };
        }

        // TURNO MAPPINGS
        public static Turno MapToTurno(TurnoCreateDto dto)
        {
            return new Turno
            {
                FechaHora = dto.FechaHora,
                Motivo = dto.Motivo,
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                Estado = "Pendiente"
            };
        }

        public static Turno MapToTurno(TurnoUpdateDto dto, Turno turno)
        {
            if (dto.FechaHora.HasValue)
                turno.FechaHora = dto.FechaHora.Value;

            if (!string.IsNullOrEmpty(dto.Motivo))
                turno.Motivo = dto.Motivo;

            if (!string.IsNullOrEmpty(dto.Estado))
                turno.Estado = dto.Estado;

            if (dto.DoctorId.HasValue)
                turno.DoctorId = dto.DoctorId.Value;

            if (!string.IsNullOrEmpty(dto.Especialidad))
                turno.Motivo = dto.Especialidad;

            return turno;
        }

        public static TurnoReadDto MapToTurnoReadDto(Turno turno)
        {
            return new TurnoReadDto
            {
                Id = turno.Id,
                FechaHora = turno.FechaHora,
                Motivo = turno.Motivo,
                Estado = turno.Estado,
                PacienteId = turno.PacienteId,
                DoctorId = turno.DoctorId,
                Especialidad = turno.Doctor?.Especialidad ?? string.Empty
            };
        }
    }
}
