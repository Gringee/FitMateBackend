using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services;

public class BodyPartService : IBodyPartService
{
    private readonly IBodyPartRepository _repo;
    public BodyPartService(IBodyPartRepository repo) => _repo = repo;

    public async Task<BodyPartDto> CreateAsync(CreateBodyPartDto dto)
    {
        var entity = dto.Adapt<BodyPart>();
        entity.BodyPartId = Guid.NewGuid();
        var created = await _repo.AddAsync(entity);
        return created.Adapt<BodyPartDto>();
    }

    public async Task<List<BodyPartDto>> GetAllAsync() => (await _repo.GetAllAsync()).Adapt<List<BodyPartDto>>();
    public async Task<BodyPartDto?> GetByIdAsync(Guid id) => (await _repo.GetByIdAsync(id))?.Adapt<BodyPartDto>();

    public async Task<bool> UpdateAsync(Guid id, UpdateBodyPartDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        dto.Adapt(entity);
        await _repo.UpdateAsync(entity);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id) => await _repo.DeleteAsync(id);
}
