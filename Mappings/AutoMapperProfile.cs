using AutoMapper;
using turnero_medico_backend.DTOs.DoctorDTOs;
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
            // ObraSocial mappings
            CreateMap<ObraSocial, ObraSocialReadDto>();
            CreateMap<ObraSocialCreateDto, ObraSocial>();
            CreateMap<ObraSocialUpdateDto, ObraSocial>();

            // Paciente mappings
            CreateMap<Paciente, PacienteReadDto>()
                .ReverseMap();

            CreateMap<PacienteCreateDto, Paciente>();

            CreateMap<PacienteUpdateDto, Paciente>();

            // Doctor mappings
            CreateMap<Doctor, DoctorReadDto>()
                .ReverseMap();

            CreateMap<DoctorCreateDto, Doctor>();

            CreateMap<DoctorUpdateDto, Doctor>();

            // Turno mappings
            CreateMap<Turno, TurnoReadDto>()
                .ForMember(dest => dest.PacienteNombre, 
                    opt => opt.MapFrom(src => src.Paciente != null ? $"{src.Paciente.Nombre} {src.Paciente.Apellido}" : "No disponible"))
                .ForMember(dest => dest.DoctorNombre, 
                    opt => opt.MapFrom(src => src.Doctor != null ? $"{src.Doctor.Nombre} {src.Doctor.Apellido}" : "No disponible"))
                .ReverseMap();

            CreateMap<TurnoCreateDto, Turno>();

            CreateMap<TurnoUpdateDto, Turno>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
