using Microsoft.EntityFrameworkCore;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.HorarioDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class HorarioServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IRepository<Doctor>> _doctorRepoMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly HorarioService _sut;

        public HorarioServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _doctorRepoMock = new Mock<IRepository<Doctor>>();
            _auditMock = new Mock<IAuditService>();

            _sut = new HorarioService(_dbContext, _doctorRepoMock.Object, _auditMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        private Doctor CrearDoctor()
        {
            return new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Test",
                Matricula = "M001", Dni = "11111111",
                Email = "dr@test.com", Telefono = "123"
            };
        }

        [Fact]
        public async Task CreateAsync_HorarioSinSuperposicion_CreaCorrectamente()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            var dto = new HorarioCreateDto
            {
                DoctorId = 1,
                DiaSemana = 1, // Lunes
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            };

            var result = await _sut.CreateAsync(dto);

            Assert.Equal(1, result.DoctorId);
            Assert.Equal(new TimeOnly(8, 0), result.HoraInicio);
        }

        [Fact]
        public async Task CreateAsync_HorarioSuperpuesto_LanzaInvalidOperation()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            // Horario existente: Lunes 08:00-12:00
            _dbContext.Horarios.Add(new Horario
            {
                DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            // Nuevo horario que se superpone: Lunes 10:00-14:00
            var dto = new HorarioCreateDto
            {
                DoctorId = 1,
                DiaSemana = 1,
                HoraInicio = new TimeOnly(10, 0),
                HoraFin = new TimeOnly(14, 0),
                DuracionMinutos = 30
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_HorariosContiguosSinSuperposicion_Permitido()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            // Horario existente: Lunes 08:00-12:00
            _dbContext.Horarios.Add(new Horario
            {
                DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            // Nuevo: Lunes 12:00-16:00 (contiguo, sin superposición)
            var dto = new HorarioCreateDto
            {
                DoctorId = 1,
                DiaSemana = 1,
                HoraInicio = new TimeOnly(12, 0),
                HoraFin = new TimeOnly(16, 0),
                DuracionMinutos = 30
            };

            var result = await _sut.CreateAsync(dto);
            Assert.Equal(new TimeOnly(12, 0), result.HoraInicio);
        }

        [Fact]
        public async Task CreateAsync_MismoDoctorDistintoDia_Permitido()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            // Horario existente: Lunes 08:00-12:00
            _dbContext.Horarios.Add(new Horario
            {
                DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            // Nuevo: Martes (2) mismo horario → no se superpone
            var dto = new HorarioCreateDto
            {
                DoctorId = 1,
                DiaSemana = 2,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            };

            var result = await _sut.CreateAsync(dto);
            Assert.Equal(2, result.DiaSemana);
        }

        [Fact]
        public async Task CreateAsync_HoraFinMenorQueInicio_LanzaInvalidOperation()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            var dto = new HorarioCreateDto
            {
                DoctorId = 1,
                DiaSemana = 1,
                HoraInicio = new TimeOnly(12, 0),
                HoraFin = new TimeOnly(8, 0),
                DuracionMinutos = 30
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task GetDisponibilidadAsync_FiltraOcupados_DevuelveSoloLibres()
        {
            var doctor = CrearDoctor();
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            // Horario: Lunes 08:00-10:00, slots de 30min → 4 slots posibles
            _dbContext.Horarios.Add(new Horario
            {
                DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(10, 0),
                DuracionMinutos = 30
            });

            var lunes = GetNextWeekday(DayOfWeek.Monday);
            var fechaBase = DateTime.SpecifyKind(lunes, DateTimeKind.Utc);

            // Turno confirmado a las 08:30
            _dbContext.Turnos.Add(new Turno
            {
                DoctorId = 1, PacienteId = 1, Motivo = "Test",
                Estado = EstadoTurno.Confirmado,
                CreatedByUserId = "u1",
                FechaHora = fechaBase.Add(new TimeSpan(8, 30, 0))
            });
            await _dbContext.SaveChangesAsync();

            var slots = (await _sut.GetDisponibilidadAsync(1, lunes)).ToList();

            // 4 slots totales (08:00, 08:30, 09:00, 09:30) menos 08:30 = 3 libres
            Assert.Equal(3, slots.Count);
            Assert.DoesNotContain(slots, s => s.FechaHora.Hour == 8 && s.FechaHora.Minute == 30);
        }

        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var date = DateTime.UtcNow.Date.AddDays(1);
            while (date.DayOfWeek != day)
                date = date.AddDays(1);
            return date;
        }
    }
}
