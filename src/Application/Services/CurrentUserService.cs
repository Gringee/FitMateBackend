using Application.Abstractions;
using Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() 
                          ?? throw new UnauthorizedAccessException("No HttpContext/User.");
}
