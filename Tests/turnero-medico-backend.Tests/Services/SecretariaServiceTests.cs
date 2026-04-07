using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class SecretariaServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ISecretariaRepository> _secretariaRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly IMapper _mapper;
        private readonly SecretariaService _sut;

        public SecretariaServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _secretariaRepoMock = new Mock<ISecretariaRepository>();
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

            _sut = new SecretariaService(
                _secretariaRepoMock.Object,
                _userManagerMock.Object,
                _currentUserMock.Object,
                _auditMock.Object,
                _dbContext,
                _mapper);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ─────────────────────────────────────────────

        private Secretaria CrearSecretariaBase(int id = 1, string? userId = null) => new()
        {
            Id = id,
            Nombre = "Ana", Apellido = "Martinez",
            Dni = "33445566", Email = "ana@test.com",
            Telefono = "3344556677",
            UserId = userId
        };

        private SecretariaCreateDto DtoCrear(string dni = "33445566") => new()
        {
            Nombre = "Ana", Apellido = "Martinez",
            Dni = dni, Email = "ana@test.com",
            Telefono = "33445566"
        };

        private SecretariaUpdateDto DtoActualizar() => new()
        {
            Id = 1, Nombre = "Ana", Apellido = "Martinez",
            Telefono = "33445566"
        };

        // ── CREATE ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_DniDuplicadoActivo_LanzaInvalidOperation()
        {
            // Secretaria activa con el mismo DNI ya existe en la BD
            _dbContext.Secretarias.Add(CrearSecretariaBase());
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear("33445566")));
        }

        [Fact]
        public async Task CreateAsync_DniDuplicadoEnSoftDeleted_LanzaInvalidOperation()
        {
            // Incluso si la secretaria fue eliminada, el DNI no puede reutilizarse
            var secretaria = CrearSecretariaBase();
            secretaria.IsDeleted = true;
            _dbContext.Secretarias.Add(secretaria);
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear("33445566")));
        }

        [Fact]
        public async Task CreateAsync_DniUnico_CreaSecretaria()
        {
            var creada = CrearSecretariaBase();
            _secretariaRepoMock.Setup(r => r.AddAsync(It.IsAny<Secretaria>())).ReturnsAsync(creada);

            var result = await _sut.CreateAsync(DtoCrear("99887766"));

            Assert.NotNull(result);
            Assert.Equal("Ana", result.Nombre);
            _secretariaRepoMock.Verify(r => r.AddAsync(It.IsAny<Secretaria>()), Times.Once);
        }

        // ── UPDATE ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_SinCuentaVinculada_ActualizaSoloDatos()
        {
            var secretaria = CrearSecretariaBase(userId: null); // sin UserId
            _secretariaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            var result = await _sut.UpdateAsync(1, DtoActualizar());

            Assert.NotNull(result);
            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ConCuentaVinculada_SincronizaNombreApellidoEnUsuario()
        {
            // Al actualizar una secretaria con cuenta, nombre y apellido deben sincronizarse
            // en AspNetUsers. El email es inmutable y no se modifica via UpdateAsync.
            var secretaria = CrearSecretariaBase(userId: "user-sec");
            _secretariaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            var user = new ApplicationUser { Id = "user-sec", Email = "ana@test.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-sec")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            await _sut.UpdateAsync(1, DtoActualizar());

            _userManagerMock.Verify(
                m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.Nombre == "Ana" && u.Apellido == "Martinez")),
                Times.Once);
        }

        // ── DELETE ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_SecretariaInexistente_LanzaKeyNotFound()
        {
            _secretariaRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Secretaria?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(99));
        }

        [Fact]
        public async Task DeleteAsync_SinCuentaVinculada_NoLlamaLockout()
        {
            var secretaria = CrearSecretariaBase(userId: null);
            _secretariaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            await _sut.DeleteAsync(1);

            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ConCuentaVinculada_LlamaLockout()
        {
            var secretaria = CrearSecretariaBase(userId: "user-sec");
            _secretariaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            var user = new ApplicationUser { Id = "user-sec" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-sec")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)).ReturnsAsync(IdentityResult.Success);

            await _sut.DeleteAsync(1);

            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        }

        // ── REACTIVAR ───────────────────────────────────────────

        [Fact]
        public async Task ReactivarAsync_YaActiva_LanzaInvalidOperation()
        {
            var secretaria = CrearSecretariaBase(); // IsDeleted = false
            _secretariaRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(secretaria);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ReactivarAsync(1));
        }

        [Fact]
        public async Task ReactivarAsync_SinCuentaVinculada_NoLlamaUnlock()
        {
            var secretaria = CrearSecretariaBase(userId: null);
            secretaria.IsDeleted = true;
            _secretariaRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            await _sut.ReactivarAsync(1);

            _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReactivarAsync_ConCuentaVinculada_LlamaUnlock()
        {
            var secretaria = CrearSecretariaBase(userId: "user-sec");
            secretaria.IsDeleted = true;
            _secretariaRepoMock.Setup(r => r.GetByIdUnscopedAsync(1)).ReturnsAsync(secretaria);
            _secretariaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Secretaria>())).ReturnsAsync(secretaria);

            var user = new ApplicationUser { Id = "user-sec" };
            _userManagerMock.Setup(m => m.FindByIdAsync("user-sec")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

            await _sut.ReactivarAsync(1);

            _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        }
    }
}
