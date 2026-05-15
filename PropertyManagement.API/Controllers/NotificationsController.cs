using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    /// <summary>
    /// API Controller: Notifications
    ///
    /// Serves in-system notifications for all roles.
    /// All endpoints are JWT-protected; each user only sees their own notifications.
    ///
    ///   Role A (Tenant) and Role B (Maintenance Staff):
    ///     GET  /api/Notifications              — list all own notifications (newest first)
    ///     GET  /api/Notifications/unread-count — unread badge count for the navbar
    ///     PUT  /api/Notifications/{id}/read    — mark single notification read
    ///     PUT  /api/Notifications/mark-all-read — mark all notifications read
    ///     DELETE /api/Notifications/{id}       — delete a single notification
    ///
    /// Notifications are created by NotificationService when business events occur
    /// (lease status changes, maintenance assignments, payment confirmations).
    /// Ownership is enforced: a user cannot read or delete another user's notification.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: api/Notifications
        /// Returns all notifications for the currently logged-in user, newest first.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.CreatedDate
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// GET: api/Notifications/unread-count
        /// Returns the count of unread notifications for the current user.
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// PUT: api/Notifications/{id}/read
        /// Marks a single notification as read. Only the owner can mark it.
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (notification.UserId != userId)
                return Forbid();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        /// <summary>
        /// PUT: api/Notifications/mark-all-read
        /// Marks all notifications for the current user as read.
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{unread.Count} notifications marked as read" });
        }

        /// <summary>
        /// DELETE: api/Notifications/{id}
        /// Deletes a notification. Only the owner can delete it.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (notification.UserId != userId)
                return Forbid();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification deleted" });
        }
    }
}
