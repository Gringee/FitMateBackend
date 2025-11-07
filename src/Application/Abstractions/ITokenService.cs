using Domain.Entities;

namespace Application.Abstractions;

public interface ITokenService
{
    (string token, DateTime expiresAtUtc) CreateAccessToken(User user, IEnumerable<string> roles);
    (string token, DateTime expiresAtUtc) CreateRefreshToken(); 
}