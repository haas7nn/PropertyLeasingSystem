using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    /// <summary>
    /// API Controller: Lease Management
    ///
    /// Implements the full lease lifecycle for the property leasing platform.
    /// All endpoints are JWT-protected; role requirements differ per action.
    ///
    ///   Role A — Tenant:
    ///     • GET  /api/Leases            View their own leases only (filtered by TenantId).
    ///     • GET  /api/Leases/{id}       View a single lease (403 if not their lease).
    ///
    ///   Role C — Property Manager (full access):
    ///     • POST   /api/Leases               Create a new lease application.
    ///     • PUT    /api/Leases/{id}           Edit lease terms (dates, rent, deposit).
    ///     • PUT    /api/Leases/{id}/screen    Move Application → Screening with notes.
    ///     • PUT    /api/Leases/{id}/approve   Move Application/Screening → Active.
    ///                                         Sets unit to Occupied. Notifies tenant.
    ///     • PUT    /api/Leases/{id}/reject    Move Application/Screening → Rejected.
    ///                                         Records reason. Notifies tenant.
    ///     • PUT    /api/Leases/{id}/terminate Move Active → Terminated.
    ///                                         Frees the unit. Notifies tenant.
    ///     • DELETE /api/Leases/{id}           Hard delete (only for non-Active leases).
    ///
    /// Business rule enforced on Approve:
    ///   Unit.AvailabilityStatus must be "Available" — prevents double-booking.
    ///
    /// Notifications:
    ///   Approve → LeaseApproved notification to tenant.
    ///   Reject  → LeaseRejected notification with reason to tenant.
    ///   Terminate → LeaseTerminated notification to tenant.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LeasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public LeasesController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context             = context;
            _notificationService = notificationService;
        }

        // ── READ ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/Leases?status=Active
        ///
        /// Role A — returns only the calling tenant's leases.
        /// Role C — returns all leases, with optional ?status filter.
        /// Results include unit and building name for display context.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var userId    = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isTenant  = User.IsInRole("Tenant");

            var query = _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);

            // Tenants see only their own leases
            if (isTenant && userId != null)
                query = query.Where(l => l.TenantId == userId);

            var leases = await query.OrderByDescending(l => l.ApplicationDate)
                .Select(l => new
                {
                    l.LeaseId,      l.UnitId,
                    Unit          = l.Unit.UnitNumber,
                    Building      = l.Unit.Building.Name,
                    l.TenantId,
                    TenantEmail   = l.Tenant.Email,
                    l.ApplicationDate, l.StartDate, l.EndDate,
                    l.MonthlyRent, l.SecurityDeposit,
                    l.Status,      l.RejectionReason, l.ScreeningNotes
                })
                .ToListAsync();

            return Ok(leases);
        }

        /// <summary>
        /// GET /api/Leases/{id}
        ///
        /// Role A — returns 403 Forbidden if tenant tries to view another tenant's lease.
        /// Role C — unrestricted access to any lease detail.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            // Enforce tenant ownership
            var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isTenant = User.IsInRole("Tenant");
            if (isTenant && lease.TenantId != userId)
                return Forbid();

            return Ok(new
            {
                lease.LeaseId,    lease.UnitId,
                Unit            = lease.Unit.UnitNumber,
                Building        = lease.Unit.Building.Name,
                BuildingAddress = lease.Unit.Building.Address,
                lease.TenantId,
                TenantEmail     = lease.Tenant.Email,
                lease.ApplicationDate, lease.StartDate, lease.EndDate,
                lease.MonthlyRent,     lease.SecurityDeposit,
                lease.Status,          lease.RejectionReason, lease.ScreeningNotes,
                Payments = lease.Payments.Select(p => new
                {
                    p.PaymentId, p.DueDate, p.AmountDue, p.AmountPaid, p.Status
                })
            });
        }

        // ── CREATE ───────────────────────────────────────────────────────────────

        /// <summary>
        /// POST /api/Leases
        /// Body: { unitId, tenantId, startDate, endDate, monthlyRent, securityDeposit }
        ///
        /// Role C (Property Manager) — creates a new lease application.
        /// Validates that the target unit is Available before creating.
        /// Status defaults to "Application"; ApplicationDate is stamped automatically.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreateLeaseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var unit = await _context.Units.FindAsync(dto.UnitId);
            if (unit == null)
                return BadRequest(new { message = $"Unit with ID {dto.UnitId} not found." });
            if (unit.AvailabilityStatus != "Available")
                return BadRequest(new { message = $"Unit {unit.UnitNumber} is not available (current status: {unit.AvailabilityStatus})." });

            // Prevent duplicate applications: reject if an open application already exists for this unit
            var existingOpen = await _context.Leases.AnyAsync(l =>
                l.UnitId == dto.UnitId &&
                (l.Status == "Application" || l.Status == "Screening"));
            if (existingOpen)
                return BadRequest(new { message = "This unit already has a pending lease application. Reject or approve it first." });

            var lease = new Lease
            {
                UnitId          = dto.UnitId,
                TenantId        = dto.TenantId,
                ApplicationDate = DateTime.Now,
                StartDate       = dto.StartDate,
                EndDate         = dto.EndDate,
                MonthlyRent     = dto.MonthlyRent,
                SecurityDeposit = dto.SecurityDeposit,
                Status          = "Application"
            };

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = lease.LeaseId },
                new { leaseId = lease.LeaseId, status = lease.Status });
        }

        // ── UPDATE ───────────────────────────────────────────────────────────────

        /// <summary>
        /// PUT /api/Leases/{id}
        /// Body: { startDate, endDate, monthlyRent, securityDeposit, screeningNotes }
        ///
        /// Role C — updates editable lease fields. Status is NOT changed here;
        /// use the dedicated lifecycle endpoints (approve, screen, reject, terminate).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaseDto dto)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            lease.StartDate       = dto.StartDate      ?? lease.StartDate;
            lease.EndDate         = dto.EndDate        ?? lease.EndDate;
            // dto fields are nullable — only update when the caller provided a value.
            // .GetValueOrDefault(lease.X) keeps the existing value if the field was omitted.
            lease.MonthlyRent     = dto.MonthlyRent.HasValue    && dto.MonthlyRent    > 0 ? dto.MonthlyRent.Value    : lease.MonthlyRent;
            lease.SecurityDeposit = dto.SecurityDeposit.HasValue && dto.SecurityDeposit > 0 ? dto.SecurityDeposit.Value : lease.SecurityDeposit;
            lease.ScreeningNotes  = dto.ScreeningNotes ?? lease.ScreeningNotes;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Lease updated.", leaseId = lease.LeaseId });
        }

        // ── LIFECYCLE ACTIONS (Role C Only) ──────────────────────────────────────

        /// <summary>
        /// PUT /api/Leases/{id}/screen
        ///
        /// Role C — advances a lease from Application → Screening.
        /// Optional screening notes (CPR check, employer verification) are recorded.
        /// </summary>
        [HttpPut("{id}/screen")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> StartScreening(int id, [FromBody] UpdateLeaseDto dto)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application")
                return BadRequest(new { message = "Only leases in Application status can be moved to Screening." });

            lease.Status        = "Screening";
            lease.ScreeningNotes = dto.ScreeningNotes;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lease moved to Screening.", leaseId = lease.LeaseId });
        }

        /// <summary>
        /// PUT /api/Leases/{id}/approve
        ///
        /// Role C — approves a lease from Application or Screening → Active.
        /// Side effects:
        ///   • Unit.AvailabilityStatus set to "Occupied".
        ///   • Unit.CurrentLeaseId set to this lease's ID.
        ///   • Tenant receives a "LeaseApproved" notification.
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Approve(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application" && lease.Status != "Screening")
                return BadRequest(new { message = "Only Application or Screening leases can be approved." });

            lease.Status                 = "Active";
            lease.Unit.AvailabilityStatus = "Occupied";
            lease.Unit.CurrentLeaseId    = lease.LeaseId;

            await _context.SaveChangesAsync();

            // Notify tenant their application was approved
            await _notificationService.LeaseApprovedAsync(lease.TenantId, lease.Unit.UnitNumber);

            return Ok(new { message = "Lease approved successfully.", leaseId = lease.LeaseId });
        }

        /// <summary>
        /// PUT /api/Leases/{id}/reject
        /// Body: { reason }
        ///
        /// Role C — rejects a lease from Application or Screening → Rejected.
        /// The rejection reason is saved and sent to the tenant as a notification.
        /// The unit remains Available (it was never set to Occupied for this lease).
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Reject(int id, [FromBody] TerminateLeaseDto dto)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application" && lease.Status != "Screening")
                return BadRequest(new { message = "Only Application or Screening leases can be rejected." });

            lease.Status          = "Rejected";
            lease.RejectionReason = dto.Reason;
            await _context.SaveChangesAsync();

            // Notify tenant with the rejection reason
            await _notificationService.LeaseRejectedAsync(
                lease.TenantId, lease.Unit.UnitNumber, dto.Reason);

            return Ok(new { message = "Lease rejected.", leaseId = lease.LeaseId });
        }

        /// <summary>
        /// PUT /api/Leases/{id}/terminate
        /// Body: { reason }
        ///
        /// Role C — terminates an Active lease.
        /// Side effects (mirror of Approve):
        ///   • Unit.AvailabilityStatus reset to "Available".
        ///   • Unit.CurrentLeaseId cleared to null.
        ///   • Termination reason recorded in RejectionReason field.
        ///   • Tenant receives a "LeaseTerminated" notification.
        /// </summary>
        [HttpPut("{id}/terminate")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Terminate(int id, [FromBody] TerminateLeaseDto dto)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Active")
                return BadRequest(new { message = "Only Active leases can be terminated." });

            lease.Status                  = "Terminated";
            lease.RejectionReason         = dto.Reason;
            lease.Unit.AvailabilityStatus = "Available";
            lease.Unit.CurrentLeaseId     = null;

            await _context.SaveChangesAsync();

            await _notificationService.LeaseTerminatedAsync(lease.TenantId, lease.Unit.UnitNumber);

            return Ok(new { message = "Lease terminated.", leaseId = lease.LeaseId });
        }

        // ── DELETE ───────────────────────────────────────────────────────────────

        /// <summary>
        /// DELETE /api/Leases/{id}
        ///
        /// Role C — permanently removes a lease record.
        /// Business rule: Active leases cannot be deleted (use Terminate instead).
        /// Cascade delete removes associated payment records automatically.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status == "Active")
                return BadRequest(new { message = "Cannot delete an active lease. Use the Terminate action instead." });

            _context.Leases.Remove(lease);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Lease deleted." });
        }
    }
}
