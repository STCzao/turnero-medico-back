using AutoMapper;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.DTOs.HorarioDTOs;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Models.Entities;

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

            // Paciente mappings
            CreateMap<Paciente, PacienteReadDto>()
                .ReverseMap();

            CreateMap<PacienteCreateDto, Paciente>();

            CreateMap<PacienteUpdateDto, Paciente>();

            // Doctor mappings
            CreateMap<Doctor, DoctorReadDto>()
                .ForMember(dest => dest.EspecialidadNombre,
                    opt => opt.MapFrom(src => src.Especialidad != null ? src.Especialidad.Nombre : string.Empty));

            CreateMap<DoctorCreateDto, Doctor>()
                .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni ?? string.Empty));

            CreateMap<DoctorUpdateDto, Doctor>()
                .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni ?? string.Empty));

            // Turno mappings
            CreateMap<Turno, TurnoReadDto>()
                .ForMember(dest => dest.PacienteNombre, 
                    opt => opt.MapFrom(src => src.Paciente != null ? $"{src.Paciente.Nombre} {src.Paciente.Apellido}" : "No disponible"))
                .ForMember(dest => dest.DoctorNombre, 
                    opt => opt.MapFrom(src => src.Doctor != null ? $"{src.Doctor.Nombre} {src.Doctor.Apellido}" : "Sin asignar"))
                .ForMember(dest => dest.EspecialidadNombre,
                    opt => opt.MapFrom(src => src.Especialidad != null ? src.Especialidad.Nombre : string.Empty));

            CreateMap<TurnoCreateDto, Turno>()
                .ForMember(dest => dest.Estado, opt => opt.Ignore())  // Estado siempre SolicitudPendiente
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore());

            CreateMap<TurnoUpdateDto, Turno>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Horario mappings
            CreateMap<Horario, HorarioReadDto>()
                .ForMember(dest => dest.DoctorNombre,
                    opt => opt.MapFrom(src => src.Doctor != null ? $"{src.Doctor.Nombre} {src.Doctor.Apellido}" : "Sin asignar"))
                .ForMember(dest => dest.DiaSemanaTexto,
                    opt => opt.MapFrom(src => DiaSemanaToString(src.DiaSemana)));

            CreateMap<HorarioCreateDto, Horario>();
        }

        private static string DiaSemanaToString(int dia) => dia switch
        {
            0 => "Domingo",
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            _ => "Desconocido"
        };
    }
}
