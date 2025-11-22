using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DbSeeder
{
    private const string AdminEmail = "admin@fitmate.local";
    private const string AdminUserName = "admin";
    private const string AdminFullName = "FitMate Admin";
    private const string AdminRoleName = "Admin";
    private const string UserRoleName = "User";
    private const string DefaultPassword = "Admin123!";
    private const string DemoPlanName = "Demo FBW";
    private const string DemoPlanType = "FBW";
    private const string DemoPlanNotes = "Seed plan";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        if (!await db.Roles.AnyAsync(ct))
        {
            db.Roles.AddRange(
                new Role { Id = Guid.NewGuid(), Name = UserRoleName },
                new Role { Id = Guid.NewGuid(), Name = AdminRoleName }
            );
            await db.SaveChangesAsync(ct);
        }
        
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail, ct);
        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                Email = AdminEmail,
                PasswordHash = passwordHasher.HashPassword(DefaultPassword),
                FullName = AdminFullName,
                UserName = AdminUserName
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);

            var adminRoleId = await db.Roles.Where(r => r.Name == AdminRoleName)
                .Select(r => r.Id)
                .FirstAsync(ct);
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRoleId });
            await db.SaveChangesAsync(ct);
        }
        
        if (!await db.Plans.AnyAsync(ct))
        {
             db.Plans.Add(new Plan {
                 Id = Guid.NewGuid(),
                 PlanName = DemoPlanName,
                 Type = DemoPlanType,
                 Notes = DemoPlanNotes,
                 CreatedByUserId = admin.Id
             });
             await db.SaveChangesAsync(ct);
        }
    }
}