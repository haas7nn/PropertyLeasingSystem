using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Controllers
{
    /// <summary>
    /// API Controller: Reports
    ///
    /// Provides aggregated reporting data consumed exclusively by the Reporting Application.
    /// All endpoints require Role C (Property Manager) — restricted by [Authorize] attribute.
    /// The Reporting App authenticates using the manager's JWT before calling these endpoints.
    ///
    ///   GET /api/Reports/occupancy        — unit availability by building with overall rate
    ///   GET /api/Reports/maintenance-stats — request counts by status, category, priority;
    ///                                        average resolution time in days
    ///   GET /api/Reports/payment-summary  — total due, collected, pending, overdue;
    ///                                        collection rate percentage
    ///
    /// No direct database access occurs in the Reporting Application — all data flows
    /// through these endpoints with JWT authentication, satisfying the project's
    /// architectural requirement for the third application tier.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PropertyManager")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // OCCUPANCY REPORT
        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancyReport()
        {
            var totalUnits = await _context.Units.CountAsync();
            var occupiedUnits = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Occupied");
            var availableUnits = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Available");
            var underMaintenance = await _context.Units.CountAsync(u => u.AvailabilityStatus == "UnderMaintenance");

            var byBuilding = await _context.Buildings
                .Select(b => new
                {
                    BuildingName = b.Name,
                    Location = b.Location,
                    TotalUnits = b.Units.Count,
                    Occupied = b.Units.Count(u => u.AvailabilityStatus == "Occupied"),
                    Available = b.Units.Count(u => u.AvailabilityStatus == "Available"),
                    OccupancyRate = b.Units.Count > 0
                        ? Math.Round((double)b.Units.Count(u => u.AvailabilityStatus == "Occupied") / (double)b.Units.Count * 100, 2)
                        : 0
                })
                .ToListAsync();

            return Ok(new
            {
                Summary = new
                {
                    TotalUnits = totalUnits,
                    OccupiedUnits = occupiedUnits,
                    AvailableUnits = availableUnits,
                    UnderMaintenance = underMaintenance,
                    OccupancyRate = totalUnits > 0 ? Math.Round((double)occupiedUnits / (double)totalUnits * 100, 2) : 0
                },
                ByBuilding = byBuilding
            });
        }

        // MAINTENANCE STATISTICS
        [HttpGet("maintenance-stats")]
        public async Task<IActionResult> GetMaintenanceStats()
        {
            var totalRequests = await _context.MaintenanceRequests.CountAsync();

            var byStatus = await _context.MaintenanceRequests
                .GroupBy(m => m.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var byCategory = await _context.MaintenanceRequests
                .GroupBy(m => m.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var byPriority = await _context.MaintenanceRequests
                .GroupBy(m => m.Priority)
                .Select(g => new
                {
                    Priority = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var closedRequests = await _context.MaintenanceRequests
                .Where(m => m.Status == "Closed" && m.ClosedDate.HasValue)
                .ToListAsync();

            var avgResolutionDays = closedRequests.Any()
                ? closedRequests.Average(m => (m.ClosedDate!.Value - m.SubmittedDate).TotalDays)
                : 0;

            return Ok(new
            {
                TotalRequests = totalRequests,
                ByStatus = byStatus,
                ByCategory = byCategory,
                ByPriority = byPriority,
                AverageResolutionDays = Math.Round(avgResolutionDays, 2)
            });
        }

        // PAYMENT SUMMARY
        [HttpGet("payment-summary")]
        public async Task<IActionResult> GetPaymentSummary()
        {
            var totalDue = await _context.Payments.SumAsync(p => p.AmountDue);
            var totalPaid = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => p.AmountPaid);
            var totalPending = await _context.Payments
                .Where(p => p.Status == "Pending")
                .SumAsync(p => p.AmountDue);
            var totalOverdue = await _context.Payments
                .Where(p => p.Status == "Overdue")
                .SumAsync(p => p.AmountDue - p.AmountPaid);

            var overdueCount = await _context.Payments.CountAsync(p => p.Status == "Overdue");

            return Ok(new
            {
                TotalDue = totalDue,
                TotalPaid = totalPaid,
                TotalPending = totalPending,
                TotalOverdue = totalOverdue,
                OverdueCount = overdueCount,
                CollectionRate = totalDue > 0 ? Math.Round((double)totalPaid / (double)totalDue * 100, 2) : 0
            });
        }
    }
}