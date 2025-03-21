using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task AddAsync(User user);
}