using PropertyManagement.API.Data;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Services
{
    /// <summary>
    /// Creates and persists in-system notifications when business events occur.
    ///
    /// Called directly by API controllers (LeasesController, MaintenanceController,
    /// PaymentsController) and MVC controllers (which share the same DbContext).
    ///
    /// Design decision: notifications are stored in the database rather than
    /// delivered via email or SMS. This keeps the system self-contained and
    /// avoids external service dependencies for the project scope.
    ///
    /// Future enhancement: these notifications could be pushed in real time via
    /// a second SignalR hub (UserNotificationsHub) instead of relying on polling.
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Core method — creates and saves a notification record for any user.
        /// All typed helper methods below call this internally.
        /// </summary>
        /// <param name="userId">The IdentityUser.Id (GUID) of the recipient.</param>
        /// <param name="message">Human-readable notification text shown in the UI.</param>
        /// <param name="type">Machine-readable event type for filtering/icons (e.g., "LeaseApproved").</param>
        public async Task CreateAsync(string userId, string message, string type)
        {
            var notification = new Notification
            {
                UserId      = userId,
                Message     = message,
                Type        = type,
                IsRead      = false,
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // ── Lease lifecycle notifications (sent to Tenant, Role A) ───────────────

        /// <summary>Sent to tenant when their lease application is approved by the manager.</summary>
        public Task LeaseApprovedAsync(string tenantId, string unitNumber)
            => CreateAsync(tenantId,
                $"Congratulations! Your lease application for unit {unitNumber} has been approved. Welcome home!",
                "LeaseApproved");

        /// <summary>Sent to tenant when their application is rejected, including the reason.</summary>
        public Task LeaseRejectedAsync(string tenantId, string unitNumber, string? reason)
            => CreateAsync(tenantId,
                $"Your lease application for unit {unitNumber} has been rejected. Reason: {reason ?? "Not specified."}",
                "LeaseRejected");

        /// <summary>Sent to tenant when an active lease is terminated by the manager.</summary>
        /// <summary>Sent to the tenant when their lease is renewed (end date extended).</summary>
        public Task LeaseRenewedAsync(string tenantId, string unitNumber, DateTime newEndDate)
            => CreateAsync(tenantId,
                $"Great news! Your lease for Unit {unitNumber} has been renewed until {newEndDate:dd MMM yyyy}.",
                "LeaseRenewed");

        public Task LeaseTerminatedAsync(string tenantId, string unitNumber)
            => CreateAsync(tenantId,
                $"Your lease for unit {unitNumber} has been terminated. Please contact the office for further details.",
                "LeaseTerminated");

        // ── Maintenance lifecycle notifications ───────────────────────────────────

        /// <summary>
        /// Sent to tenant when a staff member is assigned to their request.
        /// Informs them that work is scheduled and who will attend.
        /// </summary>
        public Task MaintenanceAssignedAsync(string tenantId, string ticketNumber, string staffEmail)
            => CreateAsync(tenantId,
                $"Your maintenance request {ticketNumber} has been assigned to {staffEmail}. They will be in touch shortly.",
                "MaintenanceAssigned");

        /// <summary>
        /// Sent to tenant when their request is marked Resolved by staff.
        /// Tenant should confirm resolution or request the ticket be reopened.
        /// </summary>
        public Task MaintenanceResolvedAsync(string tenantId, string ticketNumber)
            => CreateAsync(tenantId,
                $"Your maintenance request {ticketNumber} has been marked as Resolved. Please confirm the issue is fixed.",
                "MaintenanceResolved");

        /// <summary>
        /// Sent to the Property Manager when a tenant submits a new maintenance request from MVC.
        /// Complements the SignalR broadcast to the StaffBoard so the manager is always informed.
        /// </summary>
        public Task MaintenanceSubmittedAsync(string managerId, string ticketNumber)
            => CreateAsync(managerId,
                $"A new maintenance request has been submitted by a tenant: {ticketNumber}. Please review and assign.",
                "MaintenanceSubmitted");

        /// <summary>
        /// Sent to a staff member (Role B) when a new request in their skill area is submitted.
        /// Used alongside the SignalR real-time board broadcast for redundancy.
        /// </summary>
        public Task MaintenanceNewRequestAsync(string staffId, string ticketNumber, string category)
            => CreateAsync(staffId,
                $"A new {category} maintenance request has been submitted: {ticketNumber}.",
                "NewMaintenanceRequest");

        // ── Payment notifications (sent to Tenant, Role A) ───────────────────────

        /// <summary>Sent to tenant to confirm their payment has been received and recorded.</summary>
        public Task PaymentRecordedAsync(string tenantId, decimal amount)
            => CreateAsync(tenantId,
                $"Your rent payment of BD {amount:N2} has been received and recorded. Thank you!",
                "PaymentRecorded");

        /// <summary>
        /// Sent to tenant when a payment is flagged Overdue (past DueDate with no receipt).
        /// The property manager can also trigger this manually via the payments page.
        /// </summary>
        public Task PaymentOverdueAsync(string tenantId, decimal amount)
            => CreateAsync(tenantId,
                $"You have an overdue rent payment of BD {amount:N2}. Please contact the office to avoid penalties.",
                "PaymentOverdue");
    }
}
