using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var entity = dto.Adapt<Category>();
        entity.CategoryId = Guid.NewGuid();
        var created = await _repo.AddAsync(entity);
        return created.Adapt<CategoryDto>();
    }

    public async Task<List<CategoryDto>> GetAllAsync()
        => (await _repo.GetAllAsync()).Adapt<List<CategoryDto>>();

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
        => (await _repo.GetByIdAsync(id))?.Adapt<CategoryDto>();

    public async Task<bool> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        dto.Adapt(entity);
        await _repo.UpdateAsync(entity);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id) => await _repo.DeleteAsync(id);
}
