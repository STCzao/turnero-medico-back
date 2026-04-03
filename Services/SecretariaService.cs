using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Data;
using turnero_medico_backend.DTOs.Common;
using turnero_medico_backend.DTOs.SecretariaDTOs;
using turnero_medico_backend.Models.Entities;
using turnero_medico_backend.Repositories.Interfaces;
using turnero_medico_backend.Services.Interfaces;

namespace turnero_medico_backend.Services;

// Servicio de gestión de secretarias.
// Sigue el mismo patrón que DoctorService: el Admin crea el registro de entidad
// primero (sin cuenta), y luego registra la cuenta vía AuthService vinculando por DNI.
public class SecretariaService(
    ISecretariaRepository repository,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    ApplicationDbContext dbContext) : ISecretariaService
{
    private readonly ISecretariaRepository _repository = repository;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuditService _auditService = auditService;
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<PagedResultDto<SecretariaReadDto>> GetAllPagedAsync(int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repository.GetAllPagedAsync(page, pageSize);
        return new PagedResultDto<SecretariaReadDto>
        {
            Items = items.Select(ToDto),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SecretariaReadDto?> GetByIdAsync(int id)
    {
        var secretaria = await _repository.GetByIdAsync(id);
        return secretaria == null ? null : ToDto(secretaria);
    }

    public async Task<SecretariaReadDto?> GetMyProfileAsync()
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrEmpty(userId)) return null;

        var secretaria = await _dbContext.Secretarias
            .FirstOrDefaultAsync(s => s.UserId == userId);
        return secretaria == null ? null : ToDto(secretaria);
    }

    public async Task<SecretariaReadDto> CreateAsync(SecretariaCreateDto dto)
    {
        // IgnoreQueryFilters para detectar también soft-deleted con el mismo DNI
        var existente = await _dbContext.Secretarias
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Dni == dto.Dni.Trim());
        if (existente)
            throw new InvalidOperationException("Ya existe una secretaria con ese DNI.");

        var secretaria = new Secretaria
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dto.Dni.Trim(),
            Email = dto.Email.Trim(),
            Telefono = dto.Telefono.Trim()
        };

        var created = await _repository.AddAsync(secretaria);
        await _auditService.LogAsync(AuditAccion.Crear, "Secretaria", created.Id.ToString());
        return ToDto(created);
    }

    public async Task<SecretariaReadDto> UpdateAsync(int id, SecretariaUpdateDto dto)
    {
        var secretaria = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        secretaria.Nombre = dto.Nombre.Trim();
        secretaria.Apellido = dto.Apellido.Trim();
        secretaria.Email = dto.Email.Trim();
        secretaria.Telefono = dto.Telefono.Trim();

        // Sincronizar datos en AspNetUsers si tiene cuenta vinculada.
        // Ambas operaciones dentro de una transacción para evitar inconsistencias.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(secretaria.UserId))
            {
                var user = await _userManager.FindByIdAsync(secretaria.UserId);
                if (user != null)
                {
                    user.Nombre = secretaria.Nombre;
                    user.Apellido = secretaria.Apellido;
                    user.Email = secretaria.Email;
                    user.UserName = secretaria.Email;
                    user.NormalizedEmail = secretaria.Email.ToUpperInvariant();
                    user.NormalizedUserName = secretaria.Email.ToUpperInvariant();

                    var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Error al actualizar la cuenta de usuario: {errors}");
                    }
                }
            }

            await _repository.UpdateAsync(secretaria);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await _auditService.LogAsync(AuditAccion.Actualizar, "Secretaria", id.ToString());
        return ToDto(secretaria);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var secretaria = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            secretaria.IsDeleted = true;
            secretaria.DeletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(secretaria);

            if (!string.IsNullOrEmpty(secretaria.UserId))
            {
                var user = await _userManager.FindByIdAsync(secretaria.UserId);
                if (user != null)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await _auditService.LogAsync(AuditAccion.Eliminar, "Secretaria", id.ToString());
        return true;
    }

    public async Task<SecretariaReadDto> ReactivarAsync(int id)
    {
        var secretaria = await _repository.GetByIdUnscopedAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        if (!secretaria.IsDeleted)
            throw new InvalidOperationException("La secretaria ya se encuentra activa.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            secretaria.IsDeleted = false;
            secretaria.DeletedAt = null;
            await _repository.UpdateAsync(secretaria);

            if (!string.IsNullOrEmpty(secretaria.UserId))
            {
                var user = await _userManager.FindByIdAsync(secretaria.UserId);
                if (user != null)
                    await _userManager.SetLockoutEndDateAsync(user, null);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await _auditService.LogAsync(AuditAccion.Actualizar, "Secretaria", id.ToString());
        return ToDto(secretaria);
    }

    private static SecretariaReadDto ToDto(Secretaria s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Apellido = s.Apellido,
        Email = s.Email,
        Dni = s.Dni,
        Telefono = s.Telefono,
        TieneCuenta = !string.IsNullOrEmpty(s.UserId)
    };
}
