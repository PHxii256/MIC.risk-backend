using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.Domain;
using MIC.risk.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MIC.risk.Data
{
    public class ApplicationDBContext : IdentityDbContext<AppUser>
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions)
        {

        }
        // public DbSet<Stock> Stock { get; set; } = null!;
        // public DbSet<Comment> Comment { get; set; } = null!;

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<RiskSubCategory> RiskSubCategories => Set<RiskSubCategory>();
        public DbSet<RiskReportEvaluation> RiskReportEvaluations => Set<RiskReportEvaluation>();
        public DbSet<RiskReport> RiskReports => Set<RiskReport>();
        public DbSet<RiskReportStatusHistory> RiskReportStatusHistories => Set<RiskReportStatusHistory>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<ResourceEngagement> ResourceEngagements => Set<ResourceEngagement>();
        public DbSet<RiskAction> RiskActions => Set<RiskAction>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole
                {
                    Id = "Admin",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "1"

                },
                new IdentityRole
                {
                    Id = "User",
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp = "1"
                },
            };
            builder.Entity<IdentityRole>().HasData(roles);


            builder.Entity<Department>(e =>
            {
                e.ToTable("Department");
                e.HasKey(d => d.Id);
                e.Property(d => d.Name).IsRequired();
                e.Property(d => d.BranchLocation).IsRequired();
                e.HasIndex(d => new { d.Name, d.BranchLocation }).IsUnique();
            });

            builder.Entity<Resource>(e =>
            {
                e.ToTable("Resource");
                e.HasKey(r => r.Id);
                e.Property(r => r.Name).HasMaxLength(255).IsRequired();
                e.Property(r => r.Url).HasMaxLength(2048).IsRequired();
                e.Property(r => r.Type).HasMaxLength(50).IsRequired();
                e.Property(r => r.Description).HasMaxLength(2000);
                e.Property(r => r.Active).HasDefaultValue(true);
                e.Property(r => r.UploadedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                e.HasOne(r => r.Employee)
                    .WithMany()
                    .HasForeignKey(r => r.EmpId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.ToTable("Resource", t =>
                {
                    t.HasCheckConstraint(
                        "CK_Resource_Type",
                        "[Type] IN ('Video', 'Image', 'File', 'Quiz', 'Link')"
                    );
                });
            });

            builder.Entity<RiskSubCategory>(e =>
            {
                e.ToTable("RiskSubCategory");
                e.HasKey(sc => sc.Id);
                e.Property(sc => sc.NameEn).IsRequired();
                e.Property(sc => sc.Category).IsRequired();
                e.Property(sc => sc.Active).HasDefaultValue(true);

                e.ToTable("RiskSubCategory", t =>
               {
                   t.HasCheckConstraint(
                       "CK_RiskCategoryName",
                       "[Category] IN ('Financial', 'Operational', 'Strategic', 'Insurance')"
                   );
               });
            });

            builder.Entity<Employee>(e =>
            {
                e.ToTable("Employee");
                e.HasKey(emp => emp.Id);
                e.Property(emp => emp.Name).IsRequired();
                e.Property(emp => emp.Active).HasDefaultValue(true);
                e.Property(emp => emp.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
                e.HasIndex(emp => emp.IdentityUserId).IsUnique();

                // 1:1 relationship with ASP.NET Identity ApplicationUser
                e.HasOne(emp => emp.IdentityUser)
                    .WithOne(u => u.EmployeeProfile)
                    .HasForeignKey<Employee>(emp => emp.IdentityUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Many:1 relationship with Department
                e.HasOne(emp => emp.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(emp => emp.DeptId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RiskReportEvaluation>(e =>
            {
                e.ToTable("RiskReportEvaluation");
                e.HasKey(rre => rre.Id);

                // Inherent risk: the exposure before any credit for controls.
                e.Property(rre => rre.InherentRisk)
                    .HasComputedColumnSql(RiskScoring.InherentRiskSql, stored: true);

                // Residual risk: inherent risk carried through the control rating, 1 to 125.
                e.Property(rre => rre.ResidualRisk)
                    .HasComputedColumnSql(RiskScoring.ResidualRiskSql, stored: true);

                e.Property(rre => rre.Priority).HasDefaultValue(1);
                e.Property(rre => rre.EvaluatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                e.HasOne(rre => rre.Employee)
                    .WithMany()
                    .HasForeignKey(rre => rre.EmpId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.ToTable("RiskReportEvaluation", t =>
                {
                    t.HasCheckConstraint("CK_RiskReportEvaluation_Severity", "[Severity] BETWEEN 1 AND 5");
                    t.HasCheckConstraint("CK_RiskReportEvaluation_Frequency", "[Frequency] BETWEEN 1 AND 5");
                    t.HasCheckConstraint("CK_RiskReportEvaluation_ControlEffectiveness", "[ControlEffectiveness] BETWEEN 1 AND 5");
                    t.HasCheckConstraint("CK_RiskReportEvaluation_Priority", "[Priority] BETWEEN 1 AND 5");
                });
            });

            builder.Entity<RiskReport>(e =>
            {
                e.ToTable("RiskReport");
                e.HasKey(r => r.Id);
                e.Property(r => r.Status).HasMaxLength(50).IsRequired();
                e.Property(r => r.SubmittedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Enforce 1:1 uniqueness on evaluation references
                e.HasIndex(r => r.ReportedEvaluationId).IsUnique();
                e.HasIndex(r => r.AuditorEvaluationId)
                    .IsUnique()
                    .HasFilter("[AuditorEvaluationId] IS NOT NULL"); // Filtered index for nullables in SQL Server

                e.HasOne(r => r.Employee)
                    .WithMany()
                    .HasForeignKey(r => r.EmpId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.SubCategory)
                    .WithMany()
                    .HasForeignKey(r => r.SubCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.ReportedEvaluation)
                    .WithOne()
                    .HasForeignKey<RiskReport>(r => r.ReportedEvaluationId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.AuditorEvaluation)
                    .WithOne()
                    .HasForeignKey<RiskReport>(r => r.AuditorEvaluationId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.ToTable("RiskReport", t =>
                {
                    t.HasCheckConstraint(
                        "CK_RiskReport_Status",
                        "[Status] IN ('Submitted', 'InReview', 'Resolved', 'Archived')"
                    );
                });
            });


            builder.Entity<RiskReportStatusHistory>(e =>
            {
                e.ToTable("RiskReportStatusHistory");
                e.HasKey(h => h.Id);
                e.Property(h => h.OldStatus).HasMaxLength(50).IsRequired();
                e.Property(h => h.NewStatus).HasMaxLength(50).IsRequired();
                e.Property(h => h.ChangedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                e.HasOne(h => h.Report)
                    .WithMany(r => r.StatusHistories)
                    .HasForeignKey(h => h.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(h => h.ChangedByEmployee)
                    .WithMany()
                    .HasForeignKey(h => h.ChangedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                e.ToTable("RiskReportStatusHistory", t =>
                {
                    t.HasCheckConstraint(
                        "CK_RiskReportStatusHistory_OldStatus",
                        "[OldStatus] IN ('Submitted', 'InReview', 'Resolved', 'Archived')"
                    );

                    t.HasCheckConstraint(
                        "CK_RiskReportStatusHistory_NewStatus",
                        "[NewStatus] IN ('Submitted', 'InReview', 'Resolved', 'Archived')"
                    );
                });
            });

            builder.Entity<RiskAction>(e =>
            {
                e.ToTable("RiskAction");
                e.HasKey(a => a.Id);
                e.Property(a => a.Title).HasMaxLength(255).IsRequired();
                e.Property(a => a.Status).HasMaxLength(50).IsRequired();
                e.Property(a => a.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                e.HasOne(a => a.Report)
                    .WithMany()
                    .HasForeignKey(a => a.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.Assignee)
                    .WithMany()
                    .HasForeignKey(a => a.AssigneeEmpId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.ToTable("RiskAction", t =>
                {
                    t.HasCheckConstraint(
                        "CK_RiskAction_Status",
                        "[Status] IN ('Pending', 'Completed')"
                    );
                });
            });

            builder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshToken");
                e.HasKey(t => t.Id);

                // Fixed width: the stored value is always a hex SHA-256 digest.
                e.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
                e.Property(t => t.RevokedReason).HasMaxLength(200);
                e.Property(t => t.CreatedByIp).HasMaxLength(45);
                e.Property(t => t.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

                // Every lookup is by hash; uniqueness also guards against a hash collision
                // silently attaching one user's token to another's row.
                e.HasIndex(t => t.TokenHash).IsUnique();

                // Reuse detection revokes by family, and sign-out-everywhere revokes by user.
                e.HasIndex(t => t.FamilyId);
                e.HasIndex(t => t.UserId);

                e.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ResourceEngagement>(e =>
            {
                e.ToTable("ResourceEngagement");
                e.HasKey(re => re.Id);

                // Prevent an employee from having duplicate tracking rows for the same resource
                e.HasIndex(re => new { re.EmpId, re.ResourceId }).IsUnique();

                e.HasOne(re => re.Employee)
                    .WithMany()
                    .HasForeignKey(re => re.EmpId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(re => re.Resource)
                    .WithMany()
                    .HasForeignKey(re => re.ResourceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
