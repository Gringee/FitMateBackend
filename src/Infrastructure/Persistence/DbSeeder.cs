using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Roles.AnyAsync(ct))
        {
            db.Roles.AddRange(
                new Role { Id = Guid.NewGuid(), Name = "User" },
                new Role { Id = Guid.NewGuid(), Name = "Admin" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Users.AnyAsync(u => u.Email == "admin@fitmate.local", ct))
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@fitmate.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "FitMate Admin"
            };
            db.Users.Add(admin);

            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin", ct);
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });

            await db.SaveChangesAsync(ct);
        }
    }
}