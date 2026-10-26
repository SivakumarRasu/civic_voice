using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CivicVoice.Infrastructure.Data;

/// <summary>
/// Seeds initial Identity roles, demonstration user accounts, and applies custom SQL objects.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CivicVoiceDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        // 1. Ensure Database Created / Migrated
        await context.Database.MigrateAsync();

        // 2. Seed Identity Roles
        string[] roles = { "Citizen", "DepartmentOfficer", "Commissioner" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created Identity role: {Role}", role);
            }
        }

        // 3. Seed Demonstration Users
        var demoUsers = new[]
        {
            (Email: "citizen@civicvoice.local", Role: "Citizen", DeptId: (int?)null),
            (Email: "water_officer@civicvoice.local", Role: "DepartmentOfficer", DeptId: (int?)1),
            (Email: "roads_officer@civicvoice.local", Role: "DepartmentOfficer", DeptId: (int?)2),
            (Email: "sanitation_officer@civicvoice.local", Role: "DepartmentOfficer", DeptId: (int?)3),
            (Email: "commissioner@civicvoice.local", Role: "Commissioner", DeptId: (int?)null),
        };

        foreach (var (email, role, deptId) in demoUsers)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation("Created demo user: {Email} ({Role})", email, role);
                }
            }
        }

        // 4. Seed sample initial grievances if empty for demonstration
        if (!await context.Grievances.AnyAsync())
        {
            var citizen = await userManager.FindByEmailAsync("citizen@civicvoice.local");
            var citizenId = citizen?.Id ?? "demo-citizen-id";

            var g1 = new Grievance
            {
                CitizenId = citizenId,
                WardId = 1,
                CategoryId = 1, // Water Leakage (Dept 1, SLA 24h)
                Description = "Severe underground water pipeline leakage near Central Market crossroad causing flooding.",
                IsAnonymous = false,
                CreatedAt = DateTime.UtcNow.AddHours(-10),
                DueAt = DateTime.UtcNow.AddHours(14),
                Status = GrievanceStatus.Assigned
            };

            var g2 = new Grievance
            {
                CitizenId = citizenId,
                WardId = 2,
                CategoryId = 4, // Pothole Repair (Dept 2, SLA 48h)
                Description = "Deep dangerous pothole on main avenue near industrial gate 3 damaging two-wheelers.",
                IsAnonymous = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3), // Past SLA
                DueAt = DateTime.UtcNow.AddDays(-1), // Breached!
                Status = GrievanceStatus.InProgress
            };

            var g3 = new Grievance
            {
                CitizenId = citizenId,
                WardId = 3,
                CategoryId = 5, // Garbage Collection (Dept 3, SLA 24h)
                Description = "Overflowing community garbage bin creating foul odor outside residential complex.",
                IsAnonymous = true,
                CreatedAt = DateTime.UtcNow.AddHours(-30),
                DueAt = DateTime.UtcNow.AddHours(-6),
                Status = GrievanceStatus.Resolved
            };

            context.Grievances.AddRange(g1, g2, g3);
            await context.SaveChangesAsync();

            // Initial status histories
            context.StatusHistories.AddRange(
                new StatusHistory { GrievanceId = g1.GrievanceId, OldStatus = null, NewStatus = GrievanceStatus.Submitted, ChangedAt = g1.CreatedAt, ChangedBy = citizenId },
                new StatusHistory { GrievanceId = g1.GrievanceId, OldStatus = GrievanceStatus.Submitted, NewStatus = GrievanceStatus.Assigned, ChangedAt = g1.CreatedAt.AddHours(1), ChangedBy = "System" },
                new StatusHistory { GrievanceId = g2.GrievanceId, OldStatus = null, NewStatus = GrievanceStatus.Submitted, ChangedAt = g2.CreatedAt, ChangedBy = citizenId },
                new StatusHistory { GrievanceId = g2.GrievanceId, OldStatus = GrievanceStatus.Submitted, NewStatus = GrievanceStatus.InProgress, ChangedAt = g2.CreatedAt.AddHours(2), ChangedBy = "roads_officer@civicvoice.local" },
                new StatusHistory { GrievanceId = g3.GrievanceId, OldStatus = null, NewStatus = GrievanceStatus.Submitted, ChangedAt = g3.CreatedAt, ChangedBy = citizenId },
                new StatusHistory { GrievanceId = g3.GrievanceId, OldStatus = GrievanceStatus.Submitted, NewStatus = GrievanceStatus.Resolved, ChangedAt = g3.CreatedAt.AddHours(18), ChangedBy = "sanitation_officer@civicvoice.local" }
            );

            // Initial Assignments
            context.Assignments.AddRange(
                new Assignment { GrievanceId = g1.GrievanceId, DepartmentId = 1, AssignedAt = g1.CreatedAt },
                new Assignment { GrievanceId = g2.GrievanceId, DepartmentId = 2, AssignedAt = g2.CreatedAt },
                new Assignment { GrievanceId = g3.GrievanceId, DepartmentId = 3, AssignedAt = g3.CreatedAt }
            );

            // Initial Feedback for resolved grievance
            context.Feedbacks.Add(new Feedback
            {
                GrievanceId = g3.GrievanceId,
                CitizenId = citizenId,
                Rating = 5,
                Comments = "Thank you! The garbage was cleared promptly by the sanitation team.",
                CreatedAt = g3.CreatedAt.AddHours(20)
            });

            await context.SaveChangesAsync();
            logger.LogInformation("Seeded sample grievances, assignments, and status histories for demonstration.");
        }
    }
}
