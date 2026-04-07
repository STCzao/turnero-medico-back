using AutoMapper;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.DTOs.HorarioDTOs;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services;

namespace turnero_medico_backend.Mappings
{
    // Perfil de AutoMapper que define todas las conversiones DTO --> Entity
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Especialidad mappings
            CreateMap<Especialidad, EspecialidadReadDto>();
            CreateMap<EspecialidadCreateDto, Especialidad>();

            // ObraSocial mappings
            CreateMap<ObraSocial, ObraSocialReadDto>();

            // Secretaria mappings
            CreateMap<Secretaria, SecretariaReadDto>()
                .ForMember(dest => dest.TieneCuenta,
                    opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.UserId)));
            CreateMap<SecretariaCreateDto, Secretaria>();
            CreateMap<SecretariaUpdateDto, Secretaria>();

            // Paciente mappings
            CreateMap<Paciente, PacienteReadDto>()
                .ForMember(dest => dest.EsMayorDeEdad,
                    opt => opt.MapFrom(src => EdadHelper.EsMayorDeEdad(src.FechaNacimiento)))
                .ReverseMap();

            CreateMap<PacienteCreateDto, Paciente>();

            CreateMap<PacienteUpdateDto, Paciente>()
                .ForMember(dest => dest.Dni, opt => opt.Ignore());


            // Doctor mappings
            CreateMap<Doctor, DoctorReadDto>()
                .ForMember(dest => dest.EspecialidadNombre,
                    opt => opt.MapFrom(src => src.Especialidad != null ? src.Especialidad.Nombre : string.Empty));

            CreateMap<DoctorCreateDto, Doctor>();

            CreateMap<DoctorUpdateDto, Doctor>()
                .ForMember(dest => dest.Dni, opt => opt.Ignore());

            // Turno mappings
            // Nombres calculados desde las navigation properties; requieren que el repositorio
            // haya hecho Include() antes de mapear — de lo contrario devuelven "No disponible".
            CreateMap<Turno, TurnoReadDto>()
                .ForMember(dest => dest.PacienteNombre,
                    opt => opt.MapFrom(src => src.Paciente != null ? $"{src.Paciente.Nombre} {src.Paciente.Apellido}" : "No disponible"))
                .ForMember(dest => dest.DoctorNombre,
                    opt => opt.MapFrom(src => src.Doctor != null ? $"{src.Doctor.Nombre} {src.Doctor.Apellido}" : "Sin asignar"))
                .ForMember(dest => dest.EspecialidadNombre,
                    opt => opt.MapFrom(src => src.Especialidad != null ? src.Especialidad.Nombre : string.Empty));

            // Estado, CreatedAt y CreatedByUserId son asignados por el servicio, no por el caller.
            CreateMap<TurnoCreateDto, Turno>()
                .ForMember(dest => dest.Estado, opt => opt.Ignore())  // siempre SolicitudPendiente
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore());

            // TurnoUpdateDto solo actualiza los campos no-nulos (PATCH parcial).
            CreateMap<TurnoUpdateDto, Turno>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Horario mappings
            CreateMap<Horario, HorarioReadDto>()
                .ForMember(dest => dest.DoctorNombre,
                    opt => opt.MapFrom(src => src.Doctor != null ? $"{src.Doctor.Nombre} {src.Doctor.Apellido}" : "Sin asignar"))
                .ForMember(dest => dest.DiaSemanaTexto,
                    opt => opt.MapFrom(src => DiaSemanaHelper.ToString(src.DiaSemana)));

            CreateMap<HorarioCreateDto, Horario>();
        }
    }
}
