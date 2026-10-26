using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CivicVoice.Infrastructure.Data;

/// <summary>
/// The main Entity Framework Core DbContext for CivicVoice.
/// Inherits from IdentityDbContext to integrate ASP.NET Core Identity for authentication.
/// 
/// Key design decisions:
/// - Enum stored as integer for efficient indexing
/// - Cascade delete restricted on critical paths to prevent accidental data loss
/// - Composite unique index on Feedback(GrievanceId, CitizenId) prevents duplicate ratings
/// - Search indexes on frequently queried columns (Status, DueAt, CreatedAt, WardId, CategoryId)
/// </summary>
public class CivicVoiceDbContext : IdentityDbContext<IdentityUser>
{
    public CivicVoiceDbContext(DbContextOptions<CivicVoiceDbContext> options)
        : base(options) { }

    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Grievance> Grievances => Set<Grievance>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity tables

        // ─────────────────────────────────────────────
        //  WARD
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Ward>(e =>
        {
            e.HasKey(w => w.WardId);
            e.Property(w => w.Name).IsRequired().HasMaxLength(100);
            e.Property(w => w.Area).HasMaxLength(200);
            e.Property(w => w.Pincode).HasMaxLength(6);
        });

        // ─────────────────────────────────────────────
        //  DEPARTMENT
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.DepartmentId);
            e.Property(d => d.Name).IsRequired().HasMaxLength(150);
            e.Property(d => d.Email).IsRequired().HasMaxLength(200);

            e.HasIndex(d => d.Name).HasDatabaseName("IX_Department_Name");
        });

        // ─────────────────────────────────────────────
        //  CATEGORY
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).IsRequired().HasMaxLength(150);

            // One-to-many: Department → Category
            e.HasOne(c => c.Department)
             .WithMany(d => d.Categories)
             .HasForeignKey(c => c.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(c => c.Name).HasDatabaseName("IX_Category_Name");
        });

        // ─────────────────────────────────────────────
        //  GRIEVANCE
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Grievance>(e =>
        {
            e.ToTable(tb => tb.HasTrigger("trg_GrievanceStatusHistory"));
            e.HasKey(g => g.GrievanceId);
            e.Property(g => g.CitizenId).IsRequired().HasMaxLength(450);
            e.Property(g => g.Description).IsRequired().HasMaxLength(2000);
            e.Property(g => g.PhotoPath).HasMaxLength(500);

            // Store enum as integer for indexing and performance
            e.Property(g => g.Status)
             .HasConversion<int>()
             .HasDefaultValue(GrievanceStatus.Submitted);

            // One-to-many: Ward → Grievance
            e.HasOne(g => g.Ward)
             .WithMany(w => w.Grievances)
             .HasForeignKey(g => g.WardId)
             .OnDelete(DeleteBehavior.Restrict);

            // One-to-many: Category → Grievance
            e.HasOne(g => g.Category)
             .WithMany(c => c.Grievances)
             .HasForeignKey(g => g.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            // ── Search indexes ──
            e.HasIndex(g => g.Status).HasDatabaseName("IX_Grievance_Status");
            e.HasIndex(g => g.CreatedAt).HasDatabaseName("IX_Grievance_CreatedAt");
            e.HasIndex(g => g.DueAt).HasDatabaseName("IX_Grievance_DueAt");
            e.HasIndex(g => g.WardId).HasDatabaseName("IX_Grievance_WardId");
            e.HasIndex(g => g.CategoryId).HasDatabaseName("IX_Grievance_CategoryId");
            e.HasIndex(g => g.CitizenId).HasDatabaseName("IX_Grievance_CitizenId");

            // Composite index for SLA breach queries (Status + DueAt)
            e.HasIndex(g => new { g.Status, g.DueAt })
             .HasDatabaseName("IX_Grievance_Status_DueAt");
        });

        // ─────────────────────────────────────────────
        //  ASSIGNMENT (explicit M:N join entity)
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasKey(a => a.AssignmentId);
            e.Property(a => a.OfficerId).HasMaxLength(450);

            // One-to-many: Grievance → Assignment
            e.HasOne(a => a.Grievance)
             .WithMany(g => g.Assignments)
             .HasForeignKey(a => a.GrievanceId)
             .OnDelete(DeleteBehavior.Restrict);

            // One-to-many: Department → Assignment
            e.HasOne(a => a.Department)
             .WithMany(d => d.Assignments)
             .HasForeignKey(a => a.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─────────────────────────────────────────────
        //  STATUS HISTORY
        // ─────────────────────────────────────────────
        modelBuilder.Entity<StatusHistory>(e =>
        {
            e.HasKey(s => s.StatusHistoryId);

            e.Property(s => s.OldStatus).HasConversion<int?>();
            e.Property(s => s.NewStatus).HasConversion<int>();
            e.Property(s => s.ChangedBy).HasMaxLength(450);

            // One-to-many: Grievance → StatusHistory
            e.HasOne(s => s.Grievance)
             .WithMany(g => g.StatusHistories)
             .HasForeignKey(s => s.GrievanceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─────────────────────────────────────────────
        //  FEEDBACK
        // ─────────────────────────────────────────────
        modelBuilder.Entity<Feedback>(e =>
        {
            e.HasKey(f => f.FeedbackId);
            e.Property(f => f.CitizenId).IsRequired().HasMaxLength(450);
            e.Property(f => f.Comments).HasMaxLength(1000);

            // One-to-many: Grievance → Feedback
            e.HasOne(f => f.Grievance)
             .WithMany(g => g.Feedbacks)
             .HasForeignKey(f => f.GrievanceId)
             .OnDelete(DeleteBehavior.Restrict);

            // Unique composite index: one feedback per citizen per grievance
            e.HasIndex(f => new { f.GrievanceId, f.CitizenId })
             .IsUnique()
             .HasDatabaseName("IX_Feedback_Grievance_Citizen_Unique");
        });

        // ─────────────────────────────────────────────
        //  SEED DATA — demonstration data only
        // ─────────────────────────────────────────────
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Departments
        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, Name = "Water Supply Department", Email = "water@civicvoice.local" },
            new Department { DepartmentId = 2, Name = "Roads & Infrastructure", Email = "roads@civicvoice.local" },
            new Department { DepartmentId = 3, Name = "Sanitation & Waste Management", Email = "sanitation@civicvoice.local" },
            new Department { DepartmentId = 4, Name = "Electrical & Street Lighting", Email = "electrical@civicvoice.local" },
            new Department { DepartmentId = 5, Name = "Drainage & Sewerage", Email = "drainage@civicvoice.local" }
        );

        // Categories — each linked to a department with SLA hours
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Water Leakage", DefaultSlaHours = 24, DepartmentId = 1 },
            new Category { CategoryId = 2, Name = "Water Supply Disruption", DefaultSlaHours = 12, DepartmentId = 1 },
            new Category { CategoryId = 3, Name = "Road Damage", DefaultSlaHours = 72, DepartmentId = 2 },
            new Category { CategoryId = 4, Name = "Pothole Repair", DefaultSlaHours = 48, DepartmentId = 2 },
            new Category { CategoryId = 5, Name = "Garbage Collection", DefaultSlaHours = 24, DepartmentId = 3 },
            new Category { CategoryId = 6, Name = "Public Toilet Maintenance", DefaultSlaHours = 12, DepartmentId = 3 },
            new Category { CategoryId = 7, Name = "Streetlight Failure", DefaultSlaHours = 48, DepartmentId = 4 },
            new Category { CategoryId = 8, Name = "Electrical Hazard", DefaultSlaHours = 6, DepartmentId = 4 },
            new Category { CategoryId = 9, Name = "Drainage Blockage", DefaultSlaHours = 24, DepartmentId = 5 },
            new Category { CategoryId = 10, Name = "Sewage Overflow", DefaultSlaHours = 12, DepartmentId = 5 }
        );

        // Wards
        modelBuilder.Entity<Ward>().HasData(
            new Ward { WardId = 1, Name = "Ward 1 - Central", Area = "Central Market Area", Pincode = "500001" },
            new Ward { WardId = 2, Name = "Ward 2 - North", Area = "Industrial Zone", Pincode = "500002" },
            new Ward { WardId = 3, Name = "Ward 3 - South", Area = "Residential Colony", Pincode = "500003" },
            new Ward { WardId = 4, Name = "Ward 4 - East", Area = "IT Park Area", Pincode = "500004" },
            new Ward { WardId = 5, Name = "Ward 5 - West", Area = "Old City Area", Pincode = "500005" }
        );
    }
}
