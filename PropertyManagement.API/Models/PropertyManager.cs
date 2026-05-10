using Microsoft.AspNetCore.Identity;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Role C: Property Manager — the most senior role in the system.
    /// The Property Manager has full access to all system features:
    ///
    ///   Buildings & Units  : Create, edit, delete buildings and units.
    ///   Leases             : Manage the full lease lifecycle (Application → Active → Terminated).
    ///                        Approve or reject applications after screening.
    ///   Maintenance        : Assign requests to Maintenance Staff; close completed tickets.
    ///   Payments           : Create payment records and record receipts.
    ///   Notifications      : System generates notifications to tenants and staff on their behalf.
    ///   Reporting          : The Reporting Application is restricted to this role only.
    ///
    /// There is no separate "Administrator" role. The Property Manager handles user
    /// management and system configuration in addition to operational responsibilities.
    ///
    /// Inherits from IdentityUser — the PropertyManager role is seeded at startup
    /// with the account manager@property.com / Manager@123.
    /// </summary>
    public class PropertyManager : IdentityUser
    {
        // No additional properties beyond IdentityUser (Id, Email, PhoneNumber, etc.)
        // The role distinction is enforced via ASP.NET Core Identity role claims.
    }
}
