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

    public Task CreateUserAsync(string username, string email, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        return _repository.AddAsync(user);
    }
}