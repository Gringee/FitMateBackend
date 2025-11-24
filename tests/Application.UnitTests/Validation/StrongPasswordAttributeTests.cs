using Application.Common.Validation;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace Application.UnitTests.Validation;

public class StrongPasswordAttributeTests
{
    private StrongPasswordAttribute _validator = null!;

    public StrongPasswordAttributeTests()
    {
        _validator = new StrongPasswordAttribute();
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsStrong()
    {
        // Arrange
        var password = "MyP@ssw0rd!";
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData("Abcd1234!")]      // Valid
    [InlineData("P@ssw0rd")]       // Valid
    [InlineData("MySecure#123")]   // Valid
    [InlineData("Test$Pass1")]     // Valid
    public void Validate_ShouldPass_WhenPasswordMeetsAllRequirements(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData("short1!")]        // Too short (< 8 chars)
    [InlineData("abc")]            // Too short
    public void Validate_ShouldFail_WhenPasswordIsTooShort(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("at least 8 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsTooLong()
    {
        // Arrange - 101 characters
        var password = new string('A', 50) + new string('a', 30) + "1234567890!@#$%^&*()_+";
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("ABCDEFGH1!")]     // No lowercase
    [InlineData("TESTPASS1@")]     // No lowercase
    public void Validate_ShouldFail_WhenPasswordHasNoLowercase(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("lowercase letter");
    }

    [Theory]
    [InlineData("abcdefgh1!")]     // No uppercase
    [InlineData("testpass1@")]     // No uppercase
    public void Validate_ShouldFail_WhenPasswordHasNoUppercase(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("uppercase letter");
    }

    [Theory]
    [InlineData("Abcdefgh!")]      // No digit
    [InlineData("TestPass@")]      // No digit
    public void Validate_ShouldFail_WhenPasswordHasNoDigit(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("digit");
    }

    [Theory]
    [InlineData("Abcdefgh1")]      // No special char
    [InlineData("TestPass1")]      // No special char
    public void Validate_ShouldFail_WhenPasswordHasNoSpecialCharacter(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("special character");
    }

    [Theory]
    [InlineData("Password1!")]     // Common weak password
    [InlineData("Qwerty123!")]     // Common weak password
    [InlineData("Admin123!")]      // Common weak password
    [InlineData("Welcome1!")]      // Common weak password
    [InlineData("Passw0rd!")]      // Common weak password
    public void Validate_ShouldFail_WhenPasswordIsCommon(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("too common");
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsNull()
    {
        // Arrange - null should be handled by [Required] attribute
        var context = new ValidationContext(new { Password = (string?)null })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(null, context);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsEmpty()
    {
        // Arrange - empty should be handled by [Required] attribute
        var context = new ValidationContext(new { Password = "" })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult("", context);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData("MyP@ss123")]      // @ special char
    [InlineData("Test$ecure1")]    // $ special char
    [InlineData("Pass!word1")]     // ! special char
    [InlineData("Secure%123")]     // % special char
    [InlineData("Test*Pass1")]     // * special char
    [InlineData("Pass?word1")]     // ? special char
    [InlineData("Test&Pass1")]     // & special char
    [InlineData("Pass#word1")]     // # special char
    [InlineData("Test^Pass1")]     // ^ special char
    [InlineData("Pass(word)1")]    // () special chars
    [InlineData("Test_Pass1")]     // _ special char
    [InlineData("Pass+word1")]     // + special char
    [InlineData("Test-Pass1")]     // - special char
    [InlineData("Pass=word1")]     // = special char
    [InlineData("Test[Pass]1")]    // [] special chars
    [InlineData("Pass{word}1")]    // {} special chars
    [InlineData("Test|Pass1")]     // | special char
    [InlineData("Pass:word1")]     // : special char
    [InlineData("Test;Pass1")]     // ; special char
    [InlineData("Pass\"word1")]    // " special char
    [InlineData("Test'Pass1")]     // ' special char
    [InlineData("Pass<word>1")]    // <> special chars
    [InlineData("Test,Pass1")]     // , special char
    [InlineData("Pass.word1")]     // . special char
    [InlineData("Test/Pass1")]     // / special char
    [InlineData("Pass\\word1")]    // \ special char
    public void Validate_ShouldPass_ForAllSupportedSpecialCharacters(string password)
    {
        // Arrange
        var context = new ValidationContext(new { Password = password })
        {
            MemberName = "Password"
        };

        // Act
        var result = _validator.GetValidationResult(password, context);

        // Assert
        result.Should().Be(ValidationResult.Success, 
            $"password '{password}' should be valid with its special character");
    }
}
