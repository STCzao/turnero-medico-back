using System.ComponentModel.DataAnnotations;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.DTOs.TurnoDTOs;

namespace turnero_medico_backend.Tests.Models
{
    public class DtoValidationTests
    {
        // ── Helper ──────────────────────────────────────────────────────────────

        private static List<ValidationResult> Validate(object dto)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(dto);
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);
            return results;
        }

        // ── DependienteUpdateDto — Required con ErrorMessage en Id ──────────────

        [Fact]
        public void DependienteUpdateDto_IdCero_ErrorMessagePresente()
        {
            var dto = new DependienteUpdateDto
            {
                Id = 0, // int default → Required falla para value types solo si es nulo, pero con [Required] en int 0 no falla
                Nombre = "Juan",
                Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };
            // [Required] en un int no-nullable valida siempre (0 pasa). Verificamos que el ErrorMessage esté definido
            var attr = typeof(DependienteUpdateDto)
                .GetProperty(nameof(DependienteUpdateDto.Id))!
                .GetCustomAttributes(typeof(RequiredAttribute), false)
                .Cast<RequiredAttribute>()
                .Single();
            Assert.Equal("El ID es obligatorio", attr.ErrorMessage);
        }

        [Fact]
        public void DependienteUpdateDto_DtoValido_SinErrores()
        {
            var dto = new DependienteUpdateDto
            {
                Id = 1,
                Nombre = "Juan",
                Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };
            var errores = Validate(dto);
            Assert.Empty(errores);
        }

        // ── ConfirmarTurnoDto — FutureOrToday en FechaHora ──────────────────────

        [Fact]
        public void ConfirmarTurnoDto_FechaHoraHoy_Valido()
        {
            var dto = new ConfirmarTurnoDto
            {
                FechaHora = DateTime.UtcNow.AddHours(1)
            };
            var errores = Validate(dto);
            Assert.DoesNotContain(errores, e => e.MemberNames.Contains("FechaHora"));
        }

        [Fact]
        public void ConfirmarTurnoDto_FechaHoraEnElPasado_Invalido()
        {
            var dto = new ConfirmarTurnoDto
            {
                FechaHora = DateTime.UtcNow.AddDays(-1)
            };
            var errores = Validate(dto);
            Assert.Contains(errores, e => e.MemberNames.Contains("FechaHora"));
        }

        [Fact]
        public void ConfirmarTurnoDto_FechaHoraFutura_Valido()
        {
            var dto = new ConfirmarTurnoDto
            {
                FechaHora = DateTime.UtcNow.AddDays(7)
            };
            var errores = Validate(dto);
            Assert.DoesNotContain(errores, e => e.MemberNames.Contains("FechaHora"));
        }

        // ── DoctorCreateDto — Dni obligatorio ───────────────────────────────────

        [Fact]
        public void DoctorCreateDto_Dni_Vacio_Invalido()
        {
            var dto = DoctorCreateDtoValido();
            dto.Dni = "";
            var errores = Validate(dto);
            Assert.Contains(errores, e => e.MemberNames.Contains("Dni"));
        }

        [Fact]
        public void DoctorCreateDto_Dni_Valido_SinErrores()
        {
            var dto = DoctorCreateDtoValido();
            dto.Dni = "12345678";
            var errores = Validate(dto);
            Assert.DoesNotContain(errores, e => e.MemberNames.Contains("Dni"));
        }

        // ── DoctorUpdateDto — Matricula no editable ─────────────────────────────

        [Fact]
        public void DoctorUpdateDto_NoTienePropiedad_Matricula()
        {
            var propiedad = typeof(DoctorUpdateDto).GetProperty("Matricula");
            Assert.Null(propiedad);
        }

        // ── Email inmutable en Update DTOs ───────────────────────────────────────

        [Fact]
        public void DoctorUpdateDto_NoTienePropiedad_Email()
        {
            var propiedad = typeof(DoctorUpdateDto).GetProperty("Email");
            Assert.Null(propiedad);
        }

        [Fact]
        public void PacienteUpdateDto_NoTienePropiedad_Email()
        {
            var propiedad = typeof(PacienteUpdateDto).GetProperty("Email");
            Assert.Null(propiedad);
        }

        [Fact]
        public void SecretariaUpdateDto_NoTienePropiedad_Email()
        {
            var propiedad = typeof(SecretariaUpdateDto).GetProperty("Email");
            Assert.Null(propiedad);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static DoctorCreateDto DoctorCreateDtoValido() => new()
        {
            Matricula = "MAT001",
            Nombre = "Carlos",
            Apellido = "Lopez",
            EspecialidadId = 1,
            Email = "carlos@test.com",
            Telefono = "1122334455",
            Dni = "12345678"
        };

    }
}
