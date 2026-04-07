using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.DoctorDTOs;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class DoctorServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IDoctorRepository> _doctorRepoMock;
        private readonly Mock<IRepository<Especialidad>> _especialidadRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly IMapper _mapper;
        private readonly DoctorService _sut;

        public DoctorServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _doctorRepoMock = new Mock<IDoctorRepository>();
            _especialidadRepoMock = new Mock<IRepository<Especialidad>>();
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

            _sut = new DoctorService(
                _doctorRepoMock.Object,
                _especialidadRepoMock.Object,
                _mapper,
                _currentUserMock.Object,
                _auditMock.Object,
                _dbContext,
                _userManagerMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ─────────────────────────────────────────────

        private Doctor CrearDoctorBase(int id = 1, string? userId = null) => new()
        {
            Id = id,
            Nombre = "Carlos", Apellido = "Lopez",
            Matricula = "MAT001", Dni = "22334455",
            Email = "carlos@test.com", Telefono = "1122334455",
            EspecialidadId = 1,
            UserId = userId,
            Especialidad = new Especialidad { Id = 1, Nombre = "Cardiología" }
        };

        private DoctorCreateDto DtoCrear() => new()
        {
            Nombre = "Carlos", Apellido = "Lopez",
            Matricula = "MAT001", Dni = "22334455",
            Email = "carlos@test.com",
            Telefono = "1122334455", EspecialidadId = 1
        };

        private DoctorUpdateDto DtoActualizar(int id = 1) => new()
        {
            Id = id, Nombre = "Carlos", Apellido = "Lopez",
            Telefono = "1122334455", EspecialidadId = 1
        };

        // ── CREATE ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_EspecialidadInexistente_LanzaInvalidOperation()
        {
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Especialidad?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear()));
        }

        [Fact]
        public async Task CreateAsync_DniDuplicado_LanzaInvalidOperation()
        {
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 10, Matricula = "MAT999", Dni = "22334455",
                Nombre = "Otro", Apellido = "Doctor", Email = "otro@test.com",
                Telefono = "999999999", EspecialidadId = 1
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear()));
        }

        [Fact]
        public async Task CreateAsync_DniDuplicadoEnSoftDeleted_LanzaInvalidOperation()
        {
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 10, Matricula = "MAT999", Dni = "22334455",
                Nombre = "Otro", Apellido = "Doctor", Email = "otro@test.com",
                Telefono = "999999999", EspecialidadId = 1,
                IsDeleted = true
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear()));
        }

        [Fact]
        public async Task CreateAsync_MatriculaDuplicada_LanzaInvalidOperation()
        {
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            _dbContext.Doctores.Add(new Doctor
            {
                Id = 10, Matricula = "MAT001", Dni = "99887766", // misma matrícula, diferente DNI
                Nombre = "Otro", Apellido = "Doctor", Email = "otro@test.com",
                Telefono = "999999999", EspecialidadId = 1
            });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear()));
        }

        [Fact]
        public async Task CreateAsync_EspecialidadValida_CreaDoctor()
        {
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });

            var doctorCreado = CrearDoctorBase();
            _doctorRepoMock.Setup(r => r.AddAsync(It.IsAny<Doctor>())).ReturnsAsync(doctorCreado);
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctorCreado);

            var result = await _sut.CreateAsync(DtoCrear());

            Assert.NotNull(result);
            Assert.Equal("Carlos", result.Nombre);
            _doctorRepoMock.Verify(r => r.AddAsync(It.IsAny<Doctor>()), Times.Once);
        }

        // ── UPDATE ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_AdminActualizaCualquierDoctor_Actualiza()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Admin");

            var doctor = CrearDoctorBase(userId: "user-doctor");
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctor);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);

            var result = await _sut.UpdateAsync(1, DtoActualizar());

            Assert.NotNull(result);
            _doctorRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Doctor>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DoctorModificaSuPropioPerfil_Actualiza()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Doctor");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-doctor");

            var doctor = CrearDoctorBase(userId: "user-doctor");
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctor);
            _especialidadRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Especialidad { Id = 1, Nombre = "Cardiología" });
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);

            var result = await _sut.UpdateAsync(1, DtoActualizar());

            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_DoctorIntentaModificarOtroDoctor_LanzaUnauthorized()
        {
            _currentUserMock.Setup(x => x.GetUserRole()).Returns("Doctor");
            _currentUserMock.Setup(x => x.GetUserId()).Returns("user-otro-doctor");

            var doctor = CrearDoctorBase(userId: "user-doctor");
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctor);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.UpdateAsync(1, DtoActualizar()));
        }

        // ── DELETE ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_DoctorInexistente_LanzaKeyNotFound()
        {
            _doctorRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(99));
        }

        [Fact]
        public async Task DeleteAsync_SinCuentaVinculada_NoLlamaLockout()
        {
            var doctor = CrearDoctorBase(userId: null); // sin UserId
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);

            await _sut.DeleteAsync(1);

            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ConCuentaVinculada_LlamaLockout()
        {
            var doctor = CrearDoctorBase(userId: "user-doctor");
            _doctorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);

            var user = new ApplicationUser { Id = "user-doctor" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-doctor")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)).ReturnsAsync(IdentityResult.Success);

            await _sut.DeleteAsync(1);

            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        }

        // ── REACTIVAR ───────────────────────────────────────────

        [Fact]
        public async Task ReactivarAsync_DoctorYaActivo_LanzaInvalidOperation()
        {
            var doctor = CrearDoctorBase(); // IsDeleted = false por defecto
            _doctorRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(doctor);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReactivarAsync(1));
        }

        [Fact]
        public async Task ReactivarAsync_SinCuentaVinculada_NoLlamaUnlock()
        {
            var doctor = CrearDoctorBase(userId: null);
            doctor.IsDeleted = true;
            _doctorRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctor);

            await _sut.ReactivarAsync(1);

            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReactivarAsync_ConCuentaVinculada_LlamaUnlock()
        {
            var doctor = CrearDoctorBase(userId: "user-doctor");
            doctor.IsDeleted = true;
            _doctorRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Doctor>())).ReturnsAsync(doctor);
            _doctorRepoMock.Setup(r => r.GetByIdWithEspecialidadAsync(1)).ReturnsAsync(doctor);

            var user = new ApplicationUser { Id = "user-doctor" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-doctor")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

            await _sut.ReactivarAsync(1);

            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        }
    }
}
