using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Data
{
    /// <summary>
    /// Shared application DbContext for the entire solution.
    ///
    /// Architecture note (per project brief):
    ///   • The API project owns this DbContext and all domain entities.
    ///   • The MVC project references the API project and uses this context
    ///     directly via EF Core for all CRUD operations (no HTTP round-trips).
    ///   • The Reporting Application has NO reference to this project and
    ///     accesses data exclusively via the Web API with JWT authentication.
    ///
    /// Inherits from IdentityDbContext so Identity tables (Users, Roles, UserRoles, etc.)
    /// are included in the same database as the application tables. This avoids the
    /// complexity of managing two separate databases or DbContexts.
    ///
    /// TPH (Table-Per-Hierarchy) is used for the three user types:
    ///   Tenant, MaintenanceStaff, and PropertyManager all map to the AspNetUsers table
    ///   with a discriminator column that EF Core manages automatically.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── Application DbSets ────────────────────────────────────────────────────

        /// <summary>Buildings managed by the Property Manager.</summary>
        public DbSet<Building> Buildings { get; set; }

        /// <summary>Rentable units within buildings.</summary>
        public DbSet<Unit> Units { get; set; }

        /// <summary>
        /// Lease applications and active lease agreements.
        /// Spans the full lifecycle: Application → Screening → Active → Terminated/Rejected.
        /// </summary>
        public DbSet<Lease> Leases { get; set; }

        /// <summary>Monthly rent payment installment records.</summary>
        public DbSet<Payment> Payments { get; set; }

        /// <summary>
        /// Maintenance requests submitted by Tenants.
        /// Lifecycle: Submitted → Assigned → InProgress → Resolved → Closed.
        /// </summary>
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

        /// <summary>
        /// In-system notifications for Tenants and Maintenance Staff.
        /// Created by NotificationService when business events occur.
        /// </summary>
        public DbSet<Notification> Notifications { get; set; }

        // ── Role-specific user DbSets (TPH — all map to AspNetUsers table) ────────

        /// <summary>Role A users — tenants who lease units.</summary>
        public DbSet<Tenant> Tenants { get; set; }

        /// <summary>Role B users — maintenance technicians assigned to requests.</summary>
        public DbSet<MaintenanceStaff> MaintenanceStaffs { get; set; }

        /// <summary>Role C users — property managers with full system access.</summary>
        public DbSet<PropertyManager> PropertyManagers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suppress the warning about pending model changes — migrations are applied
            // at startup via context.Database.MigrateAsync() in SeedData.Initialize().
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Must be called first to set up Identity tables

            // ── Explicit primary keys (belt-and-suspenders alongside [Key] attributes) ─
            builder.Entity<Building>().HasKey(b => b.BuildingId);
            builder.Entity<Unit>().HasKey(u => u.UnitId);
            builder.Entity<Lease>().HasKey(l => l.LeaseId);
            builder.Entity<Payment>().HasKey(p => p.PaymentId);
            builder.Entity<MaintenanceRequest>().HasKey(m => m.RequestId);
            builder.Entity<Notification>().HasKey(n => n.NotificationId);

            // ── Rename Identity tables to cleaner names in the database ─────────────
            builder.Entity<IdentityUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // ── Building → Unit (1-to-many) ──────────────────────────────────────────
            // Restrict delete: a building cannot be removed while it has units.
            // MVC enforces this with a pre-check in DeleteConfirmed.
            builder.Entity<Unit>()
                .HasOne(u => u.Building)
                .WithMany(b => b.Units)
                .HasForeignKey(u => u.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Unit → Lease history (1-to-many) ────────────────────────────────────
            // A unit can have many historical leases (rejected, terminated, expired).
            builder.Entity<Lease>()
                .HasOne(l => l.Unit)
                .WithMany(u => u.LeaseHistory)
                .HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Unit → CurrentLease (1-to-0/1) ──────────────────────────────────────
            // Points to the single currently active lease for quick availability lookup.
            // SetNull on delete ensures CurrentLeaseId is cleared if the lease is deleted.
            builder.Entity<Unit>()
                .HasOne(u => u.CurrentLease)
                .WithOne()
                .HasForeignKey<Unit>(u => u.CurrentLeaseId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ── Tenant → Lease (1-to-many) ──────────────────────────────────────────
            // Restrict delete: tenants with lease history cannot be removed.
            builder.Entity<Lease>()
                .HasOne(l => l.Tenant)
                .WithMany(t => t.Leases)
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Lease → Payment (1-to-many, cascade delete) ─────────────────────────
            // Payments are meaningless without their lease, so cascade is appropriate.
            // The MVC controller adds a business-rule check to prevent deleting leases
            // that already have payment records.
            builder.Entity<Payment>()
                .HasOne(p => p.Lease)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LeaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Tenant → MaintenanceRequest (1-to-many) ─────────────────────────────
            // Restrict delete so tenant history is preserved even if the user is deactivated.
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.MaintenanceRequests)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Unit → MaintenanceRequest (1-to-many) ────────────────────────────────
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Unit)
                .WithMany(u => u.MaintenanceRequests)
                .HasForeignKey(m => m.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MaintenanceStaff → MaintenanceRequest (1-to-many, optional) ─────────
            // SetNull: if a staff member is removed, requests are unassigned (not deleted).
            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.AssignedStaff)
                .WithMany(s => s.AssignedRequests)
                .HasForeignKey(m => m.AssignedStaffId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ── Notification → User FK (referential integrity) ──────────────────────
            // UserId has [Required] so it is NOT NULL in the database — SetNull is
            // incompatible with a NOT NULL column (SQL Server would reject the FK).
            // Restrict: delete the user's notifications before deleting the user account,
            // or change UserId to nullable if orphaned notification rows are acceptable.
            builder.Entity<Notification>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TPH discriminator: EF Core automatically uses a column named "Discriminator"
            // which already exists in the migrated database. No explicit configuration is
            // needed — adding HasDiscriminator("UserType") here without a matching migration
            // causes SqlException: Invalid column name 'UserType'.

            // ── Unique index on TicketNumber ─────────────────────────────────────────
            // Guarantees no two maintenance requests ever share the same ticket number.
            // The generation logic in MaintenanceController.Submit() uses a daily counter
            // to further reduce the chance of conflicts under concurrent load.
            builder.Entity<MaintenanceRequest>()
                .HasIndex(m => m.TicketNumber)
                .IsUnique();

            // ── Seed Roles ───────────────────────────────────────────────────────────
            // Static IDs ensure roles are idempotent across migrations.
            // SeedData.Initialize() creates the actual user accounts at runtime.
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Tenant",           NormalizedName = "TENANT" },
                new IdentityRole { Id = "2", Name = "PropertyManager",  NormalizedName = "PROPERTYMANAGER" },
                new IdentityRole { Id = "3", Name = "MaintenanceStaff", NormalizedName = "MAINTENANCESTAFF" }
            );
        }
    }
}
