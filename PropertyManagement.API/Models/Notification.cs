using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents an in-system notification delivered to a user after a business event.
    ///
    /// Notifications are created by NotificationService when:
    ///   • A lease is Approved   → Tenant is notified (LeaseApproved)
    ///   • A lease is Rejected   → Tenant is notified (LeaseRejected)
    ///   • A lease is Terminated → Tenant is notified (LeaseTerminated)
    ///   • A request is Assigned → Tenant and Staff are notified (MaintenanceAssigned)
    ///   • A request is Resolved → Tenant is notified to confirm (MaintenanceResolved)
    ///   • A payment is Recorded → Tenant receives confirmation (PaymentRecorded)
    ///   • Payment is Overdue    → Tenant receives a reminder (PaymentOverdue)
    ///
    /// Tenants (Role A) and Staff (Role B) can mark notifications read or delete them.
    /// The API endpoint GET /api/Notifications returns only the calling user's notifications
    /// (filtered by UserId == current JWT claim) — users cannot read each other's notifications.
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        /// <summary>
        /// The ID of the user this notification belongs to (from ASP.NET Core Identity).
        /// Matches IdentityUser.Id — a GUID string. Used to filter notifications per user.
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable notification text displayed in the user's notification feed.
        /// Written by NotificationService using descriptive language that references
        /// the relevant entity (e.g., unit number, ticket number, amount).
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Category of the notification event for potential future filtering/icons.
        /// Examples: "LeaseApproved", "MaintenanceAssigned", "PaymentRecorded".
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user has read this notification.
        /// False on creation; set to true via PUT /api/Notifications/{id}/read
        /// or PUT /api/Notifications/mark-all-read.
        /// Unread count can be polled by the MVC navbar to show a badge.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// UTC timestamp when this notification was created.
        /// Used to sort notifications newest-first and for display ("2 hours ago").
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
