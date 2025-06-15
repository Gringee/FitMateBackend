using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IBodyPartService
    {
        Task<BodyPartDto> CreateAsync(CreateBodyPartDto dto);
        Task<List<BodyPartDto>> GetAllAsync();
        Task<BodyPartDto?> GetByIdAsync(Guid id);
        Task<bool> UpdateAsync(Guid id, UpdateBodyPartDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
