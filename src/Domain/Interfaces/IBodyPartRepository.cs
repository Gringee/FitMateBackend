using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IBodyPartRepository
    {
        Task<BodyPart> AddAsync(BodyPart entity);
        Task<List<BodyPart>> GetAllAsync();
        Task<BodyPart?> GetByIdAsync(Guid id);
        Task UpdateAsync(BodyPart entity);
        Task<bool> DeleteAsync(Guid id);
    }
}
