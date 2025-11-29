namespace Application.DTOs;

/// <summary>
/// Simplified user information for search results.
/// </summary>
public class UserSummaryDto
{
    /// <summary>
    /// The user's unique username.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
