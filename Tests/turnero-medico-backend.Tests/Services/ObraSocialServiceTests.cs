using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.ObraSocialDTOs;
using turnero_medico_backend.Mappings;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Services;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Tests.Services
{
    public class ObraSocialServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IAuditService> _auditMock;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly ObraSocialService _sut;

        private const string CacheKey = "obras-sociales:all";

        public ObraSocialServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _auditMock = new Mock<IAuditService>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

            _sut = new ObraSocialService(_dbContext, _mapper, _cache, _auditMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            _cache.Dispose();
            GC.SuppressFinalize(this);
        }

        // ── Helpers ─────────────────────────────────────────────

        private async Task<Especialidad> SeedEspecialidadAsync(int id = 1, string nombre = "Cardiología")
        {
            var esp = new Especialidad { Id = id, Nombre = nombre };
            _dbContext.Especialidades.Add(esp);
            await _dbContext.SaveChangesAsync();
            return esp;
        }

        private ObraSocialCreateDto DtoCrear(string nombre = "OSDE", List<int>? especialidadIds = null) =>
            new() { Nombre = nombre, EspecialidadIds = especialidadIds ?? [], Planes = [], Observaciones = "" };

        private ObraSocialUpdateDto DtoActualizar(string nombre = "OSDE Actualizada", List<int>? especialidadIds = null) =>
            new() { Nombre = nombre, EspecialidadIds = especialidadIds ?? [], Planes = [], Observaciones = "" };

        // ── CREATE ──────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NombreDuplicado_LanzaInvalidOperation()
        {
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE" });
            await _dbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(DtoCrear("OSDE")));
        }

        [Fact]
        public async Task CreateAsync_EspecialidadInexistente_LanzaInvalidOperation()
        {
            // Pedimos especialidad ID=99 que no existe
            var dto = DtoCrear("IOMA", especialidadIds: [99]);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_FlujoValido_CreaConEspecialidadesEInvalidaCache()
        {
            _cache.Set(CacheKey, new List<ObraSocialReadDto>());
            var esp = await SeedEspecialidadAsync(1, "Cardiología");

            var result = await _sut.CreateAsync(DtoCrear("OSDE", especialidadIds: [1]));

            Assert.NotNull(result);
            Assert.Equal("OSDE", result.Nombre);
            Assert.Single(result.Especialidades);
            Assert.False(_cache.TryGetValue(CacheKey, out _));
        }

        // ── UPDATE ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ObraSocialInexistente_RetornaNull()
        {
            var result = await _sut.UpdateAsync(99, DtoActualizar());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_NombreDuplicadoEnOtra_LanzaInvalidOperation()
        {
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE" });
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 2, Nombre = "IOMA" });
            await _dbContext.SaveChangesAsync();

            // Intentar renombrar OSDE (id=1) con el nombre de IOMA (id=2)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.UpdateAsync(1, DtoActualizar("IOMA")));
        }

        [Fact]
        public async Task UpdateAsync_EspecialidadInexistente_LanzaInvalidOperation()
        {
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE" });
            await _dbContext.SaveChangesAsync();

            var dto = DtoActualizar("OSDE", especialidadIds: [99]); // especialidad inexistente

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(1, dto));
        }

        [Fact]
        public async Task UpdateAsync_FlujoValido_ActualizaEspecialidadesEInvalidaCache()
        {
            _cache.Set(CacheKey, new List<ObraSocialReadDto>());
            var esp1 = await SeedEspecialidadAsync(1, "Cardiología");
            var esp2 = new Especialidad { Id = 2, Nombre = "Neurología" };
            _dbContext.Especialidades.Add(esp2);
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE", Especialidades = [esp1] });
            await _dbContext.SaveChangesAsync();

            // Actualizar: reemplazar especialidad 1 por especialidad 2
            var result = await _sut.UpdateAsync(1, DtoActualizar("OSDE Premium", especialidadIds: [2]));

            Assert.NotNull(result);
            Assert.Equal("OSDE Premium", result.Nombre);
            Assert.Single(result.Especialidades);
            Assert.Equal("Neurología", result.Especialidades.First().Nombre);
            Assert.False(_cache.TryGetValue(CacheKey, out _));
        }

        // ── DELETE ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ObraSocialInexistente_RetornaFalse()
        {
            var result = await _sut.DeleteAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_FlujoValido_EliminaEInvalidaCache()
        {
            _cache.Set(CacheKey, new List<ObraSocialReadDto>());
            _dbContext.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE" });
            await _dbContext.SaveChangesAsync();

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            Assert.Equal(0, await _dbContext.ObrasSociales.CountAsync());
            Assert.False(_cache.TryGetValue(CacheKey, out _));
        }
    }
}
