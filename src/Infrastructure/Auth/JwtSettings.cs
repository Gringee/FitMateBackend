namespace Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string Secret { get; set; } = null!; // minimum 32 chars
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}