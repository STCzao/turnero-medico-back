using Microsoft.AspNetCore.Identity;
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
    IAuditService auditService) : ISecretariaService
{
    private readonly ISecretariaRepository _repository = repository;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IAuditService _auditService = auditService;

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

    public async Task<SecretariaReadDto> CreateAsync(SecretariaCreateDto dto)
    {
        // Verificar DNI único
        var existentes = await _repository.FindAsync(s => s.Dni == dto.Dni.Trim());
        if (existentes.Any())
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

        // Verificar DNI único si cambió
        if (dto.Dni != null && dto.Dni.Trim() != secretaria.Dni)
        {
            var existentes = await _repository.FindAsync(s => s.Dni == dto.Dni.Trim() && s.Id != id);
            if (existentes.Any())
                throw new InvalidOperationException("Ya existe una secretaria con ese DNI.");
        }

        secretaria.Nombre = dto.Nombre.Trim();
        secretaria.Apellido = dto.Apellido.Trim();
        secretaria.Email = dto.Email.Trim();
        secretaria.Telefono = dto.Telefono.Trim();
        if (dto.Dni != null) secretaria.Dni = dto.Dni.Trim();

        // Sincronizar datos en AspNetUsers si tiene cuenta vinculada
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
                if (dto.Dni != null) user.Dni = secretaria.Dni;
                await _userManager.UpdateAsync(user);
            }
        }

        await _repository.UpdateAsync(secretaria);
        await _auditService.LogAsync(AuditAccion.Actualizar, "Secretaria", id.ToString());
        return ToDto(secretaria);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var secretaria = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Secretaria con ID {id} no encontrada.");

        // Si tiene cuenta vinculada, eliminarla también
        if (!string.IsNullOrEmpty(secretaria.UserId))
        {
            var user = await _userManager.FindByIdAsync(secretaria.UserId);
            if (user != null)
                await _userManager.DeleteAsync(user);
        }

        var deleted = await _repository.DeleteAsync(id);
        if (deleted)
            await _auditService.LogAsync(AuditAccion.Eliminar, "Secretaria", id.ToString());
        return deleted;
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
