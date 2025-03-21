using System;

namespace Domain;

public class User
{
	public Guid Id { get; set; }
	public string Name { get; set; } = null!;
	public string Email { get; set; }
	public string Password { get; set; }
}
