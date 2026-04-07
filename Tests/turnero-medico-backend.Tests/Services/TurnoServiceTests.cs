using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.TurnoDTOs;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class TurnoServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ITurnoRepository> _turnoRepoMock;
        private readonly Mock<IPacienteRepository> _pacienteRepoMock;
        private readonly Mock<IRepository<Doctor>> _doctorRepoMock;
        private readonly Mock<IRepository<Especialidad>> _especialidadRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly IMapper _mapper;
        private readonly TurnoService _sut;

        public TurnoServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _turnoRepoMock = new Mock<ITurnoRepository>();
            _pacienteRepoMock = new Mock<IPacienteRepository>();
            _doctorRepoMock = new Mock<IRepository<Doctor>>();
            _especialidadRepoMock = new Mock<IRepository<Especialidad>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _auditMock = new Mock<IAuditService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

            _sut = new TurnoService(
                _turnoRepoMock.Object,
                _pacienteRepoMock.Object,
                _doctorRepoMock.Object,
                _especialidadRepoMock.Object,
                _dbContext,
                _mapper,
                _currentUserMock.Object,
                _auditMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ─────────────────────────────────────────────

        private void SetupCurrentUser(string role, string userId = "user-1")
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns(role);
            _currentUserMock.Setup(x => x.GetUserId()).Returns(userId);
            _currentUserMock.Setup(x => x.IsAdmin()).Returns(role == "Admin");
        }

        private Turno CrearTurnoBase(int id = 1, string estado = EstadoTurno.SolicitudPendiente)
        {
            return new Turno
            {
                Id = id,
                PacienteId = 1,
                DoctorId = 1,
                EspecialidadId = 1,
                Motivo = "Control general",
                Estado = estado,
                CreatedByUserId = "user-1",
                CreatedAt = DateTime.UtcNow,
                Paciente = new Paciente
                {
                    Id = 1, Nombre = "Juan", Apellido = "Perez",
                    Dni = "12345678", Email = "juan@test.com",
                    Telefono = "1234567", FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                    UserId = "user-paciente"
                },
                Doctor = new Doctor
                {
                    Id = 1, Nombre = "Dr", Apellido = "Garcia",
                    Matricula = "MAT001", Dni = "87654321",
                    Email = "dr@test.com", Telefono = "7654321",
                    EspecialidadId = 1, UserId = "user-doctor"
                },
                Especialidad = new Especialidad { Id = 1, Nombre = "Cardiología" }
            };
        }

        // ── CREATE ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_SolicitudValida_CreaConEstadoPendiente()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            var paciente = new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "juan@test.com",
                Telefono = "1234567", FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = "user-paciente"
            };
            var especialidad = new Especialidad { Id = 1, Nombre = "Cardiología" };

            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(especialidad);

            var turnoCreado = CrearTurnoBase();
            _turnoRepoMock.Setup(r => r.AddAsync(It.IsAny<Turno>())).ReturnsAsync(turnoCreado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(turnoCreado);

            var dto = new TurnoCreateDto
            {
                PacienteId = 1,
                EspecialidadId = 1,
                Motivo = "Control general"
            };

            var result = await _sut.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("SolicitudPendiente", result.Estado);
            _turnoRepoMock.Verify(r => r.AddAsync(It.Is<Turno>(t => t.Estado == EstadoTurno.SolicitudPendiente)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DoctorNoPuedeCrear_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var dto = new TurnoCreateDto
            {
                PacienteId = 1,
                EspecialidadId = 1,
                Motivo = "Control"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_PacienteInexistente_LanzaInvalidOperation()
        {
            SetupCurrentUser("Paciente", "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Paciente?)null);

            var dto = new TurnoCreateDto
            {
                PacienteId = 999,
                EspecialidadId = 1,
                Motivo = "Control"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_DoctorConEspecialidadDistinta_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var paciente = new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "j@t.com", Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30)
            };
            var doctor = new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "X",
                Matricula = "M1", Dni = "11111111",
                Email = "d@t.com", Telefono = "456",
                EspecialidadId = 2 // distinta a la del turno (1)
            };

            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            var dto = new TurnoCreateDto
            {
                PacienteId = 1,
                EspecialidadId = 1,
                DoctorId = 1,
                Motivo = "Control"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
        }

        // ── CONFIRMAR ───────────────────────────────────────────

        [Fact]
        public async Task ConfirmarAsync_FlujoCompleto_CambiaEstadoAConfirmado()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            var doctor = turno.Doctor!;

            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            // Horario del doctor: Lunes (1) de 08:00 a 12:00, slots de 30min
            var lunes = GetNextWeekday(DayOfWeek.Monday);
            var fechaConfirmacion = DateTime.SpecifyKind(lunes.Add(new TimeSpan(9, 0, 0)), DateTimeKind.Utc);

            _dbContext.Horarios.Add(new Horario
            {
                Id = 1, DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            var turnoConfirmado = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            turnoConfirmado.FechaHora = fechaConfirmacion;
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoConfirmado);

            var dto = new ConfirmarTurnoDto
            {
                FechaHora = fechaConfirmacion,
                DoctorId = 1
            };

            var result = await _sut.ConfirmarAsync(1, dto);

            Assert.Equal(EstadoTurno.Confirmado, result.Estado);
            _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Estado == EstadoTurno.Confirmado)), Times.Once);
        }

        [Fact]
        public async Task ConfirmarAsync_TurnoYaConfirmado_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var dto = new ConfirmarTurnoDto { FechaHora = DateTime.UtcNow.AddDays(1) };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_FechaEnElPasado_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            var dto = new ConfirmarTurnoDto
            {
                FechaHora = DateTime.UtcNow.AddHours(-1),
                DoctorId = 1
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_SlotNoAlineado_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            var lunes = GetNextWeekday(DayOfWeek.Monday);
            // 09:15 no es múltiplo de 30 desde las 08:00 (08:00, 08:30, 09:00, 09:30...)
            var fechaDesalineada = DateTime.SpecifyKind(lunes.Add(new TimeSpan(9, 15, 0)), DateTimeKind.Utc);

            _dbContext.Horarios.Add(new Horario
            {
                Id = 1, DoctorId = 1, DiaSemana = 1,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            var dto = new ConfirmarTurnoDto { FechaHora = fechaDesalineada, DoctorId = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_PacienteIntentaConfirmar_LanzaUnauthorized()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var dto = new ConfirmarTurnoDto { FechaHora = DateTime.UtcNow.AddDays(1), DoctorId = 1 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_SinDoctorEnTurnoNiEnDto_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = new Turno
            {
                Id = 1, PacienteId = 1, DoctorId = null,
                EspecialidadId = 1, Motivo = "Control",
                Estado = EstadoTurno.SolicitudPendiente,
                CreatedByUserId = "user-1", CreatedAt = DateTime.UtcNow
            };
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var dto = new ConfirmarTurnoDto { FechaHora = DateTime.UtcNow.AddDays(1), DoctorId = null };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_DoctorConEspecialidadDistintaAlTurno_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente); // EspecialidadId = 1
            var doctorOtraEsp = new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "X",
                Matricula = "M1", Dni = "11111111",
                Email = "d@t.com", Telefono = "456",
                EspecialidadId = 2 // distinta al turno
            };

            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctorOtraEsp);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => new Especialidad { Id = id, Nombre = $"Especialidad {id}" });

            var dto = new ConfirmarTurnoDto
            {
                FechaHora = DateTime.UtcNow.AddDays(1),
                DoctorId = 1
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_FueraDeHorarioAtencion_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            // Horario configurado solo para Lunes, pero la fecha es Martes
            var martes = GetNextWeekday(DayOfWeek.Tuesday);
            var fechaFueraDeHorario = DateTime.SpecifyKind(martes.Add(new TimeSpan(9, 0, 0)), DateTimeKind.Utc);

            _dbContext.Horarios.Add(new Horario
            {
                Id = 1, DoctorId = 1, DiaSemana = (int)DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });
            await _dbContext.SaveChangesAsync();

            var dto = new ConfirmarTurnoDto { FechaHora = fechaFueraDeHorario, DoctorId = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        [Fact]
        public async Task ConfirmarAsync_ConflictoConTurnoConfirmadoExistente_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            var lunes = GetNextWeekday(DayOfWeek.Monday);
            var fechaConflicto = DateTime.SpecifyKind(lunes.Add(new TimeSpan(9, 0, 0)), DateTimeKind.Utc);

            _dbContext.Horarios.Add(new Horario
            {
                Id = 1, DoctorId = 1, DiaSemana = (int)DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0),
                DuracionMinutos = 30
            });

            // Turno confirmado del mismo doctor en el mismo slot
            _dbContext.Turnos.Add(new Turno
            {
                Id = 99, DoctorId = 1, PacienteId = 2,
                Estado = EstadoTurno.Confirmado,
                FechaHora = fechaConflicto,
                CreatedByUserId = "user-1"
            });
            await _dbContext.SaveChangesAsync();

            var dto = new ConfirmarTurnoDto { FechaHora = fechaConflicto, DoctorId = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConfirmarAsync(1, dto));
        }

        // ── RECHAZAR ────────────────────────────────────────────

        [Fact]
        public async Task RechazarAsync_DesdePendiente_CambiaEstado()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var turnoRechazado = CrearTurnoBase(estado: EstadoTurno.Rechazado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoRechazado);

            var result = await _sut.RechazarAsync(1, new RechazarTurnoDto { MotivoRechazo = "Sin cobertura" });

            Assert.Equal(EstadoTurno.Rechazado, result.Estado);
        }

        [Fact]
        public async Task RechazarAsync_DesdeConfirmado_LanzaInvalidOperation()
        {
            SetupCurrentUser("Secretaria", "user-sec");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RechazarAsync(1, new RechazarTurnoDto { MotivoRechazo = "Razón" }));
        }

        // ── CANCELAR ────────────────────────────────────────────

        [Theory]
        [InlineData(EstadoTurno.SolicitudPendiente)]
        [InlineData(EstadoTurno.Confirmado)]
        public async Task CancelarAsync_DesdeEstadoCancelable_CambiaEstado(string estadoInicial)
        {
            SetupCurrentUser("Admin", "user-admin");

            var turno = CrearTurnoBase(estado: estadoInicial);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var turnoCancelado = CrearTurnoBase(estado: EstadoTurno.Cancelado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoCancelado);

            var result = await _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo" });

            Assert.Equal(EstadoTurno.Cancelado, result.Estado);
        }

        [Theory]
        [InlineData(EstadoTurno.Completado)]
        [InlineData(EstadoTurno.Rechazado)]
        [InlineData(EstadoTurno.Ausente)]
        [InlineData(EstadoTurno.Cancelado)]
        public async Task CancelarAsync_DesdeEstadoFinal_LanzaInvalidOperation(string estadoFinal)
        {
            SetupCurrentUser("Admin", "user-admin");

            var turno = CrearTurnoBase(estado: estadoFinal);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo" }));
        }

        [Fact]
        public async Task CancelarAsync_DoctorCancelaConfirmado_CambiaEstado()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            var turnoCancelado = CrearTurnoBase(estado: EstadoTurno.Cancelado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoCancelado);

            var result = await _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo doctor" });

            Assert.Equal(EstadoTurno.Cancelado, result.Estado);
        }

        [Fact]
        public async Task CancelarAsync_DoctorIntentaCancelarPendiente_LanzaInvalidOperation()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            // Doctor solo puede cancelar Confirmados — SolicitudPendiente no tiene doctor asignado todavía
            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo" }));
        }

        [Fact]
        public async Task CancelarAsync_DoctorAjenoAlTurno_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-otro-doctor");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo" }));
        }

        [Fact]
        public async Task CancelarAsync_PacienteCancelaPropio_CambiaEstado()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            _dbContext.Pacientes.Add(turno.Paciente!);
            await _dbContext.SaveChangesAsync();

            var turnoCancelado = CrearTurnoBase(estado: EstadoTurno.Cancelado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoCancelado);

            var result = await _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "No puedo ir" });

            Assert.Equal(EstadoTurno.Cancelado, result.Estado);
        }

        [Fact]
        public async Task CancelarAsync_PacienteAjenoAlTurno_LanzaUnauthorized()
        {
            SetupCurrentUser("Paciente", "user-otro-paciente");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            _dbContext.Pacientes.Add(turno.Paciente!);
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _sut.CancelarAsync(1, new CancelarTurnoDto { Motivo = "Motivo" }));
        }

        // ── UPDATE (Doctor: Completado/Ausente) ─────────────────

        [Fact]
        public async Task UpdateAsync_DoctorMarcaCompletado_FlujoValido()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            var turnoCompletado = CrearTurnoBase(estado: EstadoTurno.Completado);
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turnoCompletado);

            var dto = new TurnoUpdateDto { Id = 1, Estado = EstadoTurno.Completado };
            var result = await _sut.UpdateAsync(1, dto);

            Assert.Equal(EstadoTurno.Completado, result.Estado);
        }

        [Fact]
        public async Task UpdateAsync_DoctorIntentaCompletarPendiente_LanzaInvalidOperation()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase(estado: EstadoTurno.SolicitudPendiente);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            var dto = new TurnoUpdateDto { Id = 1, Estado = EstadoTurno.Completado };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(1, dto));
        }

        [Fact]
        public async Task UpdateAsync_EstadoInvalido_LanzaInvalidOperation()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase(estado: EstadoTurno.Confirmado);
            _turnoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno);

            var dto = new TurnoUpdateDto { Id = 1, Estado = EstadoTurno.Confirmado };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(1, dto));
        }

        // ── GET BY DOCTOR (ramas de autorización) ───────────────

        [Fact]
        public async Task GetByDoctorAsync_AdminPuedeVerTurnosDeOtroDoctor_RetornaLista()
        {
            SetupCurrentUser("Admin", "user-admin");

            var turnos = new List<Turno> { CrearTurnoBase() };
            _turnoRepoMock.Setup(r => r.FindWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Turno, bool>>>()))
                .ReturnsAsync(turnos);

            var result = await _sut.GetByDoctorAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetByDoctorAsync_DoctorPuedeVerSusPropiosTurnos_RetornaLista()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var doctor = new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Garcia",
                Matricula = "M1", Dni = "11111111",
                Email = "dr@t.com", Telefono = "456",
                EspecialidadId = 1, UserId = "user-doctor"
            };
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            var turnos = new List<Turno> { CrearTurnoBase() };
            _turnoRepoMock.Setup(r => r.FindWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Turno, bool>>>()))
                .ReturnsAsync(turnos);

            var result = await _sut.GetByDoctorAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetByDoctorAsync_DoctorAjenoAlDoctor_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-otro-doctor");

            var doctor = new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Garcia",
                Matricula = "M1", Dni = "11111111",
                Email = "dr@t.com", Telefono = "456",
                EspecialidadId = 1, UserId = "user-doctor" // distinto al usuario actual
            };
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByDoctorAsync(1));
        }

        [Fact]
        public async Task GetByDoctorAsync_PacienteIntentaConsultar_LanzaUnauthorized()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByDoctorAsync(1));
        }

        // ── GET BY ID (ramas de autorización) ───────────────────

        [Fact]
        public async Task GetByIdAsync_AdminPuedeVerCualquierTurno_RetornaDto()
        {
            SetupCurrentUser("Admin", "user-admin");

            var turno = CrearTurnoBase();
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_PacientePuedeVerSuPropiTurno_RetornaDto()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            var turno = CrearTurnoBase(); // PacienteId = 1
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);

            _dbContext.Pacientes.Add(new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "j@t.com", Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = "user-paciente"
            });
            await _dbContext.SaveChangesAsync();

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetByIdAsync_PacienteAjenoAlTurno_LanzaUnauthorized()
        {
            SetupCurrentUser("Paciente", "user-otro-paciente");

            var turno = CrearTurnoBase(); // PacienteId = 1, UserId = "user-paciente"
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);

            _dbContext.Pacientes.Add(new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "j@t.com", Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = "user-paciente"
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByIdAsync(1));
        }

        [Fact]
        public async Task GetByIdAsync_DoctorPuedeVerSuPropiTurno_RetornaDto()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase(); // DoctorId = 1, Doctor.UserId = "user-doctor"
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetByIdAsync_DoctorAjenoAlTurno_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-otro-doctor");

            var turno = CrearTurnoBase(); // Doctor.UserId = "user-doctor"
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turno.Doctor!);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByIdAsync(1));
        }

        [Fact]
        public async Task GetByIdAsync_DoctorConTurnoSinDoctorAsignado_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            var turno = CrearTurnoBase();
            turno.DoctorId = null;
            _turnoRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(turno);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByIdAsync(1));
        }

        // ── GET HISTORIAL (ramas de autorización) ────────────────

        [Fact]
        public async Task GetHistorialAsync_AdminPuedeVerCualquierHistorial_RetornaCompletados()
        {
            SetupCurrentUser("Admin", "user-admin");

            var turnoCompletado = CrearTurnoBase(estado: EstadoTurno.Completado);
            _turnoRepoMock.Setup(r => r.FindWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Turno, bool>>>()))
                .ReturnsAsync(new List<Turno> { turnoCompletado });

            var result = await _sut.GetHistorialAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetHistorialAsync_PacientePuedeVerSuHistorial_RetornaCompletados()
        {
            SetupCurrentUser("Paciente", "user-paciente");

            _dbContext.Pacientes.Add(new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "j@t.com", Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = "user-paciente"
            });
            await _dbContext.SaveChangesAsync();

            var turnoCompletado = CrearTurnoBase(estado: EstadoTurno.Completado);
            _turnoRepoMock.Setup(r => r.FindWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Turno, bool>>>()))
                .ReturnsAsync(new List<Turno> { turnoCompletado });

            var result = await _sut.GetHistorialAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetHistorialAsync_PacienteAjenoAlHistorial_LanzaUnauthorized()
        {
            SetupCurrentUser("Paciente", "user-otro-paciente");

            _dbContext.Pacientes.Add(new Paciente
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez",
                Dni = "12345678", Email = "j@t.com", Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = "user-paciente"
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetHistorialAsync(1));
        }

        [Fact]
        public async Task GetHistorialAsync_DoctorConTurnoCompartido_PuedeVerHistorial()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Garcia",
                Matricula = "M1", Dni = "11111111",
                Email = "dr@t.com", Telefono = "456",
                EspecialidadId = 1, UserId = "user-doctor"
            });
            _dbContext.Turnos.Add(new Turno
            {
                Id = 10, DoctorId = 1, PacienteId = 1,
                Estado = EstadoTurno.Completado,
                CreatedByUserId = "user-1"
            });
            await _dbContext.SaveChangesAsync();

            var turnoCompletado = CrearTurnoBase(estado: EstadoTurno.Completado);
            _turnoRepoMock.Setup(r => r.FindWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Turno, bool>>>()))
                .ReturnsAsync(new List<Turno> { turnoCompletado });

            var result = await _sut.GetHistorialAsync(1);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetHistorialAsync_DoctorSinTurnoConPaciente_LanzaUnauthorized()
        {
            SetupCurrentUser("Doctor", "user-doctor");

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Garcia",
                Matricula = "M1", Dni = "11111111",
                Email = "dr@t.com", Telefono = "456",
                EspecialidadId = 1, UserId = "user-doctor"
            });
            // Sin turnos compartidos entre doctor 1 y paciente 1
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetHistorialAsync(1));
        }

        // ── Flujo completo: Solicitud → Confirmado → Completado ─

        [Fact]
        public void FlujoCompleto_SolicitudConfirmadoCompletado_TransicionesValidas()
        {
            // Serie de transiciones del happy path
            EstadoTurno.ValidarTransicion(EstadoTurno.SolicitudPendiente, EstadoTurno.Confirmado);
            EstadoTurno.ValidarTransicion(EstadoTurno.Confirmado, EstadoTurno.Completado);

            // Verificar que Completado es estado final
            Assert.True(EstadoTurno.EsEstadoFinal(EstadoTurno.Completado));
        }

        // ── Helpers privados ────────────────────────────────────

        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var date = DateTime.UtcNow.Date.AddDays(1);
            while (date.DayOfWeek != day)
                date = date.AddDays(1);
            return date;
        }
    }
}
