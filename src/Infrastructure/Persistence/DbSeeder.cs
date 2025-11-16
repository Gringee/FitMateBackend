using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Roles.AnyAsync(ct))
        {
            db.Roles.AddRange(
                new Role { Id = Guid.NewGuid(), Name = "User" },
                new Role { Id = Guid.NewGuid(), Name = "Admin" }
            );
            await db.SaveChangesAsync(ct);
        }
        
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@fitmate.local", ct);
        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@fitmate.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "FitMate Admin",
                UserName = "admin"
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);

            var adminRoleId = await db.Roles.Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstAsync(ct);
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRoleId });
            await db.SaveChangesAsync(ct);
        }
        
        if (!await db.Plans.AnyAsync(ct))
        {
             db.Plans.Add(new Plan {
                 Id = Guid.NewGuid(),
                 PlanName = "Demo FBW",
                 Type = "FBW",
                 Notes = "Seed plan",
                 CreatedByUserId = admin.Id
             });
             await db.SaveChangesAsync(ct);
        }
    }
}