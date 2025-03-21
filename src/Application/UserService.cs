using Domain;

namespace Application;

public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public Task<List<User>> GetAllUsersAsync()
        => _repository.GetAllAsync();

    public Task CreateUserAsync(string name, string email, string password)
        => _repository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Password = password
        });
}