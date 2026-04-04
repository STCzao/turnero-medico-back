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
