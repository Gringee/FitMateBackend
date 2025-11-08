using System.Security.Claims;

namespace Application.Common.Security;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        // ClaimTypes.NameIdentifier (ASP.NET) lub "sub" (standard JWT)
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value
                  ?? throw new UnauthorizedAccessException("Missing user id claim.");

        if (!Guid.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Invalid user id claim format.");

        return id;
    }
}