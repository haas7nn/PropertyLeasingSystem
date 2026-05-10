using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Note: MigrateAsync() is NOT called here — Program.cs calls it before
            // invoking SeedData.Initialize(), so this is safe to skip.
            // Calling MigrateAsync() twice is harmless but redundant and slower.

            // ===== SEED ROLES =====
            string[] roleNames = { "Tenant", "PropertyManager", "MaintenanceStaff" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // ===== SEED PROPERTY MANAGER =====
            if (await userManager.FindByEmailAsync("manager@property.com") == null)
            {
                var manager = new PropertyManager
                {
                    UserName = "manager@property.com",
                    Email = "manager@property.com",
                    EmailConfirmed = true,
                    PhoneNumber = "39111222"
                };

                var result = await userManager.CreateAsync(manager, "Manager@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(manager, "PropertyManager");
                }
            }

            // ===== SEED TENANTS =====
            var tenantData = new[]
            {
                new { Email = "ali@tenant.com", Phone = "39999111", CPR = "920101111", Occupation = "Engineer" },
                new { Email = "fatima@tenant.com", Phone = "39999222", CPR = "930202222", Occupation = "Teacher" },
                new { Email = "sara@tenant.com", Phone = "39999333", CPR = "940303333", Occupation = "Doctor" },
                new { Email = "ahmed@tenant.com", Phone = "39999444", CPR = "950404444", Occupation = "Accountant" }
            };

            foreach (var data in tenantData)
            {
                if (await userManager.FindByEmailAsync(data.Email) == null)
                {
                    var tenant = new Tenant
                    {
                        UserName = data.Email,
                        Email = data.Email,
                        PhoneNumber = data.Phone,
                        CPR = data.CPR,
                        Occupation = data.Occupation,
                        EmailConfirmed = true,
                        RegistrationDate = DateTime.UtcNow.AddMonths(-6)
                    };

                    var result = await userManager.CreateAsync(tenant, "Tenant@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(tenant, "Tenant");
                    }
                }
            }

            // ===== SEED MAINTENANCE STAFF =====
            var staffData = new[]
            {
                new { Email = "ahmed@staff.com", Phone = "39888111", Skills = "[\"Plumbing\", \"Electrical\"]" },
                new { Email = "mohammed@staff.com", Phone = "39888222", Skills = "[\"HVAC\", \"Structural\"]" },
                new { Email = "yousif@staff.com", Phone = "39888333", Skills = "[\"Electrical\", \"HVAC\"]" }
            };

            foreach (var data in staffData)
            {
                if (await userManager.FindByEmailAsync(data.Email) == null)
                {
                    var staff = new MaintenanceStaff
                    {
                        UserName = data.Email,
                        Email = data.Email,
                        PhoneNumber = data.Phone,
                        Skills = data.Skills,
                        AvailabilityStatus = "Available",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(staff, "Staff@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(staff, "MaintenanceStaff");
                    }
                }
            }

            // ===== SEED BUILDINGS =====
            if (!context.Buildings.Any())
            {
                var buildings = new[]
                {
                    new Building
                    {
                        Name = "Sunset Tower",
                        Address = "Block 123, Road 45, Manama",
                        Location = "Manama"
                    },
                    new Building
                    {
                        Name = "Marina Heights",
                        Address = "Block 456, Road 78, Seef",
                        Location = "Seef"
                    },
                    new Building
                    {
                        Name = "Pearl Plaza",
                        Address = "Block 789, Road 12, Riffa",
                        Location = "Riffa"
                    }
                };

                context.Buildings.AddRange(buildings);
                await context.SaveChangesAsync();
            }

            // ===== SEED UNITS =====
            if (!context.Units.Any())
            {
                var building1 = await context.Buildings.FirstAsync(b => b.Name == "Sunset Tower");
                var building2 = await context.Buildings.FirstAsync(b => b.Name == "Marina Heights");
                var building3 = await context.Buildings.FirstAsync(b => b.Name == "Pearl Plaza");

                var units = new List<Unit>
                {
                    // Sunset Tower Units (4 units)
                    new Unit { BuildingId = building1.BuildingId, UnitNumber = "101", Type = "Apartment", Bedrooms = 2, Bathrooms = 2, SizeInSqFt = 1200, MonthlyRent = 400, Amenities = "[\"Parking\", \"Pool\", \"Gym\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building1.BuildingId, UnitNumber = "102", Type = "Apartment", Bedrooms = 3, Bathrooms = 2, SizeInSqFt = 1500, MonthlyRent = 500, Amenities = "[\"Parking\", \"Balcony\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building1.BuildingId, UnitNumber = "103", Type = "Apartment", Bedrooms = 1, Bathrooms = 1, SizeInSqFt = 800, MonthlyRent = 300, Amenities = "[\"Parking\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building1.BuildingId, UnitNumber = "201", Type = "Apartment", Bedrooms = 2, Bathrooms = 2, SizeInSqFt = 1200, MonthlyRent = 420, Amenities = "[\"Parking\", \"Pool\"]", AvailabilityStatus = "Available" },

                    // Marina Heights Units (3 units)
                    new Unit { BuildingId = building2.BuildingId, UnitNumber = "101", Type = "Office", Bedrooms = null, Bathrooms = 1, SizeInSqFt = 600, MonthlyRent = 350, Amenities = "[\"Parking\", \"Reception\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building2.BuildingId, UnitNumber = "102", Type = "Office", Bedrooms = null, Bathrooms = 2, SizeInSqFt = 1000, MonthlyRent = 500, Amenities = "[\"Conference Room\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building2.BuildingId, UnitNumber = "G01", Type = "Shop", Bedrooms = null, Bathrooms = 1, SizeInSqFt = 400, MonthlyRent = 600, Amenities = "[\"Street Facing\"]", AvailabilityStatus = "Available" },

                    // Pearl Plaza Units (3 units)
                    new Unit { BuildingId = building3.BuildingId, UnitNumber = "101", Type = "Apartment", Bedrooms = 2, Bathrooms = 2, SizeInSqFt = 1100, MonthlyRent = 380, Amenities = "[\"Parking\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building3.BuildingId, UnitNumber = "102", Type = "Apartment", Bedrooms = 3, Bathrooms = 2, SizeInSqFt = 1400, MonthlyRent = 480, Amenities = "[\"Parking\", \"Garden\"]", AvailabilityStatus = "Available" },
                    new Unit { BuildingId = building3.BuildingId, UnitNumber = "201", Type = "Apartment", Bedrooms = 1, Bathrooms = 1, SizeInSqFt = 750, MonthlyRent = 280, Amenities = "[\"Balcony\"]", AvailabilityStatus = "Available" }
                };

                context.Units.AddRange(units);
                await context.SaveChangesAsync();
            }

            // ===== SEED LEASES =====
            if (!context.Leases.Any())
            {
                var ali = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "ali@tenant.com");
                var fatima = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "fatima@tenant.com");
                var sara = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "sara@tenant.com");

                var unit101 = await context.Units.FirstAsync(u => u.UnitNumber == "101" && u.Building.Name == "Sunset Tower");
                var unit102 = await context.Units.FirstAsync(u => u.UnitNumber == "102" && u.Building.Name == "Sunset Tower");
                var unit201 = await context.Units.FirstAsync(u => u.UnitNumber == "201" && u.Building.Name == "Sunset Tower");

                var leases = new[]
                {
                    // Active Lease
                    new Lease
                    {
                        UnitId = unit101.UnitId,
                        TenantId = ali.Id,
                        ApplicationDate = DateTime.UtcNow.AddMonths(-2),
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow.AddMonths(11),
                        MonthlyRent = unit101.MonthlyRent,
                        SecurityDeposit = unit101.MonthlyRent,
                        Status = "Active"
                    },
                    // Expired Lease
                    new Lease
                    {
                        UnitId = unit102.UnitId,
                        TenantId = fatima.Id,
                        ApplicationDate = DateTime.UtcNow.AddMonths(-14),
                        StartDate = DateTime.UtcNow.AddMonths(-13),
                        EndDate = DateTime.UtcNow.AddMonths(-1),
                        MonthlyRent = unit102.MonthlyRent,
                        SecurityDeposit = unit102.MonthlyRent,
                        Status = "Terminated"
                    },
                    // Pending Renewal Lease
                    new Lease
                    {
                        UnitId = unit201.UnitId,
                        TenantId = sara.Id,
                        ApplicationDate = DateTime.UtcNow.AddMonths(-13),
                        StartDate = DateTime.UtcNow.AddMonths(-12),
                        EndDate = DateTime.UtcNow.AddDays(30),
                        MonthlyRent = unit201.MonthlyRent,
                        SecurityDeposit = unit201.MonthlyRent,
                        Status = "Active"  // Will be marked for renewal
                    }
                };

                context.Leases.AddRange(leases);
                await context.SaveChangesAsync();

                // Update unit statuses
                unit101.AvailabilityStatus = "Occupied";
                unit101.CurrentLeaseId = leases[0].LeaseId;

                unit102.AvailabilityStatus = "Available"; // Expired lease

                unit201.AvailabilityStatus = "Occupied";
                unit201.CurrentLeaseId = leases[2].LeaseId;

                await context.SaveChangesAsync();
            }

            // ===== SEED PAYMENTS =====
            if (!context.Payments.Any())
            {
                var activeLease = await context.Leases.FirstAsync(l => l.Status == "Active" && l.Tenant.Email == "ali@tenant.com");

                var payments = new[]
                {
                    // Paid Payment
                    new Payment
                    {
                        LeaseId = activeLease.LeaseId,
                        DueDate = DateTime.UtcNow.AddDays(-25),
                        AmountDue = activeLease.MonthlyRent,
                        AmountPaid = activeLease.MonthlyRent,
                        PaymentDate = DateTime.UtcNow.AddDays(-25),
                        Status = "Paid",
                        PaymentMethod = "Bank Transfer",
                        ReceiptNumber = "RCP-001"
                    },
                    // Pending Payment
                    new Payment
                    {
                        LeaseId = activeLease.LeaseId,
                        DueDate = DateTime.UtcNow.AddDays(5),
                        AmountDue = activeLease.MonthlyRent,
                        AmountPaid = 0,
                        Status = "Pending"
                    },
                    // Overdue Payment
                    new Payment
                    {
                        LeaseId = activeLease.LeaseId,
                        DueDate = DateTime.UtcNow.AddDays(-10),
                        AmountDue = activeLease.MonthlyRent,
                        AmountPaid = 0,
                        Status = "Overdue"
                    }
                };

                context.Payments.AddRange(payments);
                await context.SaveChangesAsync();
            }

            // ===== SEED MAINTENANCE REQUESTS =====
            if (!context.MaintenanceRequests.Any())
            {
                var ali = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "ali@tenant.com");
                var sara = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "sara@tenant.com");
                var fatima = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "fatima@tenant.com");

                var ahmedStaff = await context.Users.OfType<MaintenanceStaff>().FirstAsync(s => s.Email == "ahmed@staff.com");
                var mohammedStaff = await context.Users.OfType<MaintenanceStaff>().FirstAsync(s => s.Email == "mohammed@staff.com");

                var unit101 = await context.Units.FirstAsync(u => u.UnitNumber == "101" && u.Building.Name == "Sunset Tower");
                var unit201 = await context.Units.FirstAsync(u => u.UnitNumber == "201" && u.Building.Name == "Sunset Tower");
                var unit102Marina = await context.Units.FirstAsync(u => u.UnitNumber == "102" && u.Building.Name == "Marina Heights");

                // Generate a stable base prefix from the current date (YYMMDD)
                var datePrefix = DateTime.UtcNow.ToString("yyMMdd");

                var requests = new[]
                {
                    // Submitted
                    new MaintenanceRequest
                    {
                        TicketNumber = $"MNT-{datePrefix}-01",
                        TenantId = ali.Id,
                        UnitId = unit101.UnitId,
                        Category = "Plumbing",
                        Priority = "High",
                        Description = "Kitchen sink is leaking continuously",
                        SubmittedDate = DateTime.UtcNow.AddHours(-2),
                        Status = "Submitted"
                    },
                    // Assigned
                    new MaintenanceRequest
                    {
                        TicketNumber = $"MNT-{datePrefix}-02",
                        TenantId = sara.Id,
                        UnitId = unit201.UnitId,
                        Category = "Electrical",
                        Priority = "Medium",
                        Description = "Bedroom light fixture not working",
                        SubmittedDate = DateTime.UtcNow.AddDays(-1),
                        Status = "Assigned",
                        AssignedStaffId = ahmedStaff.Id
                    },
                    // InProgress
                    new MaintenanceRequest
                    {
                        TicketNumber = $"MNT-{datePrefix}-03",
                        TenantId = fatima.Id,
                        UnitId = unit102Marina.UnitId,
                        Category = "HVAC",
                        Priority = "Urgent",
                        Description = "Air conditioning not cooling",
                        SubmittedDate = DateTime.UtcNow.AddDays(-3),
                        Status = "InProgress",
                        AssignedStaffId = mohammedStaff.Id
                    },
                    // Resolved
                    new MaintenanceRequest
                    {
                        TicketNumber = $"MNT-{datePrefix}-04",
                        TenantId = ali.Id,
                        UnitId = unit101.UnitId,
                        Category = "Electrical",
                        Priority = "Low",
                        Description = "Living room outlet not working",
                        SubmittedDate = DateTime.UtcNow.AddDays(-5),
                        Status = "Resolved",
                        AssignedStaffId = ahmedStaff.Id,
                        ResolutionNotes = "Replaced faulty outlet. Tested and working."
                    },
                    // Closed
                    new MaintenanceRequest
                    {
                        TicketNumber = $"MNT-{datePrefix}-05",
                        TenantId = sara.Id,
                        UnitId = unit201.UnitId,
                        Category = "Plumbing",
                        Priority = "Medium",
                        Description = "Bathroom faucet dripping",
                        SubmittedDate = DateTime.UtcNow.AddDays(-10),
                        Status = "Closed",
                        AssignedStaffId = ahmedStaff.Id,
                        ResolutionNotes = "Replaced washer. Issue resolved.",
                        ClosedDate = DateTime.UtcNow.AddDays(-8)
                    }
                };

                context.MaintenanceRequests.AddRange(requests);
                await context.SaveChangesAsync();
            }

            // ===== SEED NOTIFICATIONS =====
            if (!context.Notifications.Any())
            {
                var ali = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "ali@tenant.com");
                var sara = await context.Users.OfType<Tenant>().FirstAsync(t => t.Email == "sara@tenant.com");
                var ahmedStaff = await context.Users.OfType<MaintenanceStaff>().FirstAsync(s => s.Email == "ahmed@staff.com");

                var notifications = new[]
                {
                    new Notification
                    {
                        UserId = ali.Id,
                        Message = "Your lease for Unit 101 has been approved",
                        Type = "LeaseApproval",
                        IsRead = true,
                        CreatedDate = DateTime.UtcNow.AddDays(-30)
                    },
                    new Notification
                    {
                        UserId = ali.Id,
                        Message = "Payment due on " + DateTime.UtcNow.AddDays(5).ToShortDateString(),
                        Type = "PaymentDue",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow.AddHours(-12)
                    },
                    new Notification
                    {
                        UserId = sara.Id,
                        Message = "Your maintenance request has been assigned to staff",
                        Type = "MaintenanceAssigned",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new Notification
                    {
                        UserId = ahmedStaff.Id,
                        Message = "New maintenance request assigned to you - Unit 201",
                        Type = "MaintenanceAssigned",
                        IsRead = true,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new Notification
                    {
                        UserId = ali.Id,
                        Message = "Your maintenance request has been resolved",
                        Type = "MaintenanceResolved",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    }
                };

                context.Notifications.AddRange(notifications);
                await context.SaveChangesAsync();
            }
        }
    }
}