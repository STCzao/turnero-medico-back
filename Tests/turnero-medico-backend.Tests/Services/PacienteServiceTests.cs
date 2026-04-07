using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.DTOs.PacienteDTOs;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class PacienteServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IPacienteRepository> _pacienteRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly IMapper _mapper;
        private readonly PacienteService _sut;

        public PacienteServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _pacienteRepoMock = new Mock<IPacienteRepository>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _auditMock = new Mock<IAuditService>();

            var store = new Mock<IUserStore<ApplicationUser>>();
#pragma warning disable CS8625
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

            _sut = new PacienteService(
                _pacienteRepoMock.Object,
                _dbContext,
                _mapper,
                _currentUserMock.Object,
                _auditMock.Object,
                _userManagerMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        private static Paciente CrearPaciente(int id = 1, string? userId = null, bool isDeleted = false)
        {
            return new Paciente
            {
                Id = id,
                Nombre = "Juan",
                Apellido = "Perez",
                Dni = "12345678",
                Email = "juan@test.com",
                Telefono = "123",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30),
                UserId = userId,
                IsDeleted = isDeleted
            };
        }

        // ── DeleteAsync ─────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_PacienteConTurnosActivos_LanzaInvalidOperation()
        {
            var paciente = CrearPaciente();
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            _dbContext.Turnos.Add(new Turno
            {
                DoctorId = 1, PacienteId = 1, Motivo = "Test",
                Estado = EstadoTurno.Confirmado,
                CreatedByUserId = "u1"
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_PacienteSinCuenta_MarcaSoftDeleteSinLlamarUserManager()
        {
            var paciente = CrearPaciente(userId: null);
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>()))
                .ReturnsAsync((Paciente p) => p);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            _pacienteRepoMock.Verify(r => r.UpdateAsync(It.Is<Paciente>(p => p.IsDeleted)), Times.Once);
            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_PacienteConCuenta_LlamaLockoutYMarcaSoftDelete()
        {
            var paciente = CrearPaciente(userId: "user-1");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>()))
                .ReturnsAsync((Paciente p) => p);

            var user = new ApplicationUser { Id = "user-1" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, true))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            _pacienteRepoMock.Verify(r => r.UpdateAsync(It.Is<Paciente>(p => p.IsDeleted)), Times.Once);
            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        }

        // ── GetByIdAsync (ramas de autorización) ───────────────────

        [Fact]
        public async Task GetByIdAsync_AdminPuedeVerCualquierPaciente_RetornaDto()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Admin");
            var paciente = CrearPaciente();
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetByIdAsync_DoctorConTurnoCompartido_PuedeVerPaciente()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Doctor");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-doctor");

            var paciente = CrearPaciente();
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Test", Matricula = "M1",
                Dni = "11111111", Email = "dr@test.com", Telefono = "123456789",
                EspecialidadId = 1, UserId = "user-doctor"
            });
            _dbContext.Turnos.Add(new Turno
            {
                Id = 1, DoctorId = 1, PacienteId = 1,
                Estado = EstadoTurno.Completado, CreatedByUserId = "user-doctor"
            });
            await _dbContext.SaveChangesAsync();

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetByIdAsync_DoctorSinTurnoConPaciente_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Doctor");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-doctor");

            var paciente = CrearPaciente();
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 1, Nombre = "Dr", Apellido = "Test", Matricula = "M1",
                Dni = "11111111", Email = "dr@test.com", Telefono = "123456789",
                EspecialidadId = 1, UserId = "user-doctor"
            });
            // Sin turnos entre este doctor y el paciente
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByIdAsync(1));
        }

        [Fact]
        public async Task GetByIdAsync_PacientePuedeVerSuPropioPerfil_RetornaDto()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-paciente");

            var paciente = CrearPaciente(userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetByIdAsync_PacienteAjenoAlPerfil_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-otro");

            var paciente = CrearPaciente(userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetByIdAsync(1));
        }

        // ── UpdateAsync ─────────────────────────────────────────────

        private static PacienteUpdateDto DtoActualizar() => new()
        {
            Id = 1, Nombre = "Juan", Apellido = "Perez",
            Telefono = "12345678",
            FechaNacimiento = DateTime.UtcNow.AddYears(-30)
        };

        [Fact]
        public async Task UpdateAsync_AdminActualizaCualquierPaciente_Actualiza()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Admin");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-admin");

            var paciente = CrearPaciente(userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(paciente);

            var result = await _sut.UpdateAsync(1, DtoActualizar());

            Assert.NotNull(result);
            _pacienteRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Paciente>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_PacienteModificaSuPropioPerfil_Actualiza()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-paciente");

            var paciente = CrearPaciente(userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(paciente);

            var result = await _sut.UpdateAsync(1, DtoActualizar());

            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_PacienteIntentaModificarPerfilAjeno_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-otro");

            var paciente = CrearPaciente(userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paciente);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.UpdateAsync(1, DtoActualizar()));
        }

        // ── CreateAsync ─────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_FlujoValido_CreaYRetornaDto()
        {
            var paciente = CrearPaciente();
            _pacienteRepoMock.Setup(r => r.AddAsync(It.IsAny<Paciente>())).ReturnsAsync(paciente);

            var dto = new PacienteCreateDto
            {
                Dni = "12345678", Nombre = "Juan", Apellido = "Perez",
                Email = "juan@test.com", Telefono = "12345678",
                FechaNacimiento = DateTime.UtcNow.AddYears(-30)
            };

            var result = await _sut.CreateAsync(dto);

            Assert.NotNull(result);
            _pacienteRepoMock.Verify(r => r.AddAsync(It.IsAny<Paciente>()), Times.Once);
        }

        // ── CreateDependienteAsync ──────────────────────────────────

        [Fact]
        public async Task CreateDependienteAsync_DniDuplicado_LanzaInvalidOperation()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-paciente");
            _pacienteRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Paciente, bool>>>()))
                .ReturnsAsync(new List<Paciente> { CrearPaciente() }); // DNI ya existe

            var dto = new DependienteCreateDto
            {
                Dni = "12345678", Nombre = "Hijo", Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateDependienteAsync(dto));
        }

        [Fact]
        public async Task CreateDependienteAsync_UsuarioEsDependienteDeOtro_LanzaInvalidOperation()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-dependiente");
            _pacienteRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Paciente, bool>>>()))
                .ReturnsAsync(new List<Paciente>())           // DNI: no hay duplicado
                .ReturnsAsync(new List<Paciente> { CrearPaciente() }); // usuario es dependiente

            var dto = new DependienteCreateDto
            {
                Dni = "99887766", Nombre = "Nieto", Apellido = "Test",
                FechaNacimiento = DateTime.UtcNow.AddYears(-5)
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateDependienteAsync(dto));
        }

        [Fact]
        public async Task CreateDependienteAsync_FlujoValido_CreaConResponsableId()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-responsable");
            _pacienteRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Paciente, bool>>>()))
                .ReturnsAsync(new List<Paciente>())  // DNI: no hay duplicado
                .ReturnsAsync(new List<Paciente>()); // usuario no es dependiente

            var dependiente = new Paciente
            {
                Id = 2, Dni = "99887766", Nombre = "Hijo", Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10),
                ResponsableId = "user-responsable", Telefono = string.Empty
            };
            _pacienteRepoMock.Setup(r => r.AddAsync(It.IsAny<Paciente>())).ReturnsAsync(dependiente);

            var dto = new DependienteCreateDto
            {
                Dni = "99887766", Nombre = "Hijo", Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };

            var result = await _sut.CreateDependienteAsync(dto);

            Assert.NotNull(result);
            _pacienteRepoMock.Verify(
                r => r.AddAsync(It.Is<Paciente>(p => p.ResponsableId == "user-responsable" && p.UserId == null)),
                Times.Once);
        }

        // ── UpdateDependienteAsync ──────────────────────────────────

        [Fact]
        public async Task UpdateDependienteAsync_ResponsableAjeno_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-otro");

            var dependiente = CrearPaciente();
            dependiente.ResponsableId = "user-responsable"; // dueño diferente
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dependiente);

            var dto = new DependienteUpdateDto
            {
                Id = 1, Nombre = "Hijo", Apellido = "Test",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.UpdateDependienteAsync(1, dto));
        }

        [Fact]
        public async Task UpdateDependienteAsync_ResponsablePropio_Actualiza()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-responsable");

            var dependiente = CrearPaciente();
            dependiente.ResponsableId = "user-responsable";
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dependiente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(dependiente);

            var dto = new DependienteUpdateDto
            {
                Id = 1, Nombre = "Hijo", Apellido = "Perez",
                FechaNacimiento = DateTime.UtcNow.AddYears(-10)
            };

            var result = await _sut.UpdateDependienteAsync(1, dto);

            Assert.NotNull(result);
            _pacienteRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Paciente>()), Times.Once);
        }

        // ── DeleteDependienteAsync ──────────────────────────────────

        [Fact]
        public async Task DeleteDependienteAsync_AdminPuedeEliminarCualquier()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Admin");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-admin");

            var dependiente = CrearPaciente();
            dependiente.ResponsableId = "user-otro"; // admin elimina aunque no sea el responsable
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dependiente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(dependiente);

            var result = await _sut.DeleteDependienteAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteDependienteAsync_ResponsableAjeno_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-otro");

            var dependiente = CrearPaciente();
            dependiente.ResponsableId = "user-responsable";
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dependiente);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteDependienteAsync(1));
        }

        [Fact]
        public async Task DeleteDependienteAsync_ResponsablePropioElimina()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Paciente");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-responsable");

            var dependiente = CrearPaciente();
            dependiente.ResponsableId = "user-responsable";
            _pacienteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dependiente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(dependiente);

            var result = await _sut.DeleteDependienteAsync(1);

            Assert.True(result);
            _pacienteRepoMock.Verify(
                r => r.UpdateAsync(It.Is<Paciente>(p => p.IsDeleted)), Times.Once);
        }

        // ── ExportarMisDatosAsync ───────────────────────────────────

        [Fact]
        public async Task ExportarMisDatosAsync_SinPacienteVinculado_RetornaNull()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-sin-paciente");
            _pacienteRepoMock.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Paciente, bool>>>()))
                .ReturnsAsync((Paciente?)null);

            var result = await _sut.ExportarMisDatosAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task ExportarMisDatosAsync_ConPacienteYTurnos_RetornaExportConTurnos()
        {
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-paciente");

            var paciente = CrearPaciente(id: 1, userId: "user-paciente");
            _pacienteRepoMock.Setup(r => r.FindFirstAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Paciente, bool>>>()))
                .ReturnsAsync(paciente);

            _dbContext.Turnos.Add(new Turno
            {
                Id = 1, PacienteId = 1, Estado = EstadoTurno.Completado,
                Motivo = "Control", CreatedByUserId = "user-paciente"
            });
            await _dbContext.SaveChangesAsync();

            var result = await _sut.ExportarMisDatosAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Single(result.Turnos);
        }

        // ── ReactivarAsync ──────────────────────────────────────────

        [Fact]
        public async Task ReactivarAsync_PacienteYaActivo_LanzaInvalidOperation()
        {
            var paciente = CrearPaciente(isDeleted: false);
            _pacienteRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(paciente);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReactivarAsync(1));
        }

        [Fact]
        public async Task ReactivarAsync_PacienteSoftDeleted_LimpiaIsDeletedSinLlamarUserManager()
        {
            var paciente = CrearPaciente(userId: null, isDeleted: true);
            _pacienteRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>()))
                .ReturnsAsync((Paciente p) => p);

            await _sut.ReactivarAsync(1);

            _pacienteRepoMock.Verify(
                r => r.UpdateAsync(It.Is<Paciente>(p => !p.IsDeleted && p.DeletedAt == null)),
                Times.Once);
            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReactivarAsync_PacienteConCuenta_LlamaUnlockYLimpiaIsDeleted()
        {
            var paciente = CrearPaciente(userId: "user-1", isDeleted: true);
            _pacienteRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(paciente);
            _pacienteRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>()))
                .ReturnsAsync((Paciente p) => p);

            var user = new ApplicationUser { Id = "user-1" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null))
                .ReturnsAsync(IdentityResult.Success);

            await _sut.ReactivarAsync(1);

            _pacienteRepoMock.Verify(
                r => r.UpdateAsync(It.Is<Paciente>(p => !p.IsDeleted && p.DeletedAt == null)),
                Times.Once);
            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        }
    }
}
