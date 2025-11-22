namespace Application.Common;

public static class ValidationConstants
{
    public const string UserNamePattern = @"^[A-Za-z0-9._-]+$";
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 50;
    public const string UserNameErrorMessage = "UserName may contain only letters, digits, dot, underscore, and hyphen.";
}
