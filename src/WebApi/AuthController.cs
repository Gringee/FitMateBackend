using Application;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok("Registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _authService.LoginAsync(request.Email, request.Password);
        if (token == null)
            return Unauthorized(new { message = "Incorrect email or password!" });

        return Ok(new { token });
    }
}

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
