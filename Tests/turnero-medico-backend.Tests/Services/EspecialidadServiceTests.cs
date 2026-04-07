using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.DTOs.EspecialidadDTOs;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class EspecialidadServiceTests : IDisposable
    {
        private readonly Mock<IRepository<Especialidad>> _repoMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly EspecialidadService _sut;

        private const string CacheKey = "especialidades:all";

        public EspecialidadServiceTests()
        {
            _repoMock = new Mock<IRepository<Especialidad>>();
            _auditMock = new Mock<IAuditService>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

            _sut = new EspecialidadService(_repoMock.Object, _mapper, _cache, _auditMock.Object);
        }

        public void Dispose()
        {
            _cache.Dispose();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ─────────────────────────────────────────────

        private static Especialidad EspBase(int id = 1, string nombre = "Cardiología") =>
            new() { Id = id, Nombre = nombre };

        // ── GET ALL (caché) ──────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_SinCache_ConsultaDbYAlmacenaEnCache()
        {
            var lista = new List<Especialidad> { EspBase() };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(lista);

            await _sut.GetAllAsync();
            await _sut.GetAllAsync(); // segunda llamada: debería salir del caché

            // El repositorio solo debería llamarse una vez
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ConCacheCaliente_NoConsultaDb()
        {
            // Pre-poblar el caché directamente
            _cache.Set(CacheKey, new List<EspecialidadReadDto> { new() { Id = 1, Nombre = "Cardiología" } });

            await _sut.GetAllAsync();

            _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
        }

        // ── CREATE ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NombreDuplicado_LanzaInvalidOperation()
        {
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Especialidad, bool>>>()))
                .ReturnsAsync(new List<Especialidad> { EspBase() });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CreateAsync(new EspecialidadCreateDto { Nombre = "Cardiología" }));
        }

        [Fact]
        public async Task CreateAsync_NombreUnico_CreaEInvalidaCache()
        {
            // Pre-poblar caché para verificar que se invalida
            _cache.Set(CacheKey, new List<EspecialidadReadDto>());

            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Especialidad, bool>>>()))
                .ReturnsAsync(new List<Especialidad>());
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Especialidad>())).ReturnsAsync(EspBase());

            var result = await _sut.CreateAsync(new EspecialidadCreateDto { Nombre = "Cardiología" });

            Assert.NotNull(result);
            Assert.False(_cache.TryGetValue(CacheKey, out _)); // caché invalidado
        }

        // ── UPDATE ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_EspecialidadInexistente_RetornaNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Especialidad?)null);

            var result = await _sut.UpdateAsync(99, new EspecialidadUpdateDto { Nombre = "Nueva" });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_NombreDuplicadoEnOtra_LanzaInvalidOperation()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(EspBase(1, "Cardiología"));
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Especialidad, bool>>>()))
                .ReturnsAsync(new List<Especialidad> { EspBase(2, "Neurología") }); // otro con mismo nombre

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.UpdateAsync(1, new EspecialidadUpdateDto { Nombre = "Neurología" }));
        }

        [Fact]
        public async Task UpdateAsync_FlujoValido_ActualizaEInvalidaCache()
        {
            _cache.Set(CacheKey, new List<EspecialidadReadDto>());

            var esp = EspBase(1, "Cardiología");
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(esp);
            _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Especialidad, bool>>>()))
                .ReturnsAsync(new List<Especialidad>());
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Especialidad>())).ReturnsAsync(esp);

            var result = await _sut.UpdateAsync(1, new EspecialidadUpdateDto { Nombre = "Cardiología Intervencionista" });

            Assert.NotNull(result);
            Assert.False(_cache.TryGetValue(CacheKey, out _));
        }

        // ── DELETE ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_EspecialidadInexistente_RetornaFalse()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Especialidad?)null);

            var result = await _sut.DeleteAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_FlujoValido_EliminaEInvalidaCache()
        {
            _cache.Set(CacheKey, new List<EspecialidadReadDto>());

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(EspBase());
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            Assert.False(_cache.TryGetValue(CacheKey, out _));
        }
    }
}
