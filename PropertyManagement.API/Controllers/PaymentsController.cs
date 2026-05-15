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
    /// API Controller: Payments
    ///
    /// Manages rent payment records for active leases.
    ///
    ///   Role A (Tenant):
    ///     GET /api/Payments  — view own payment history only (filtered by lease.TenantId)
    ///     GET /api/Payments/{id} — view a single payment (403 if not their lease)
    ///
    ///   Role C (Property Manager):
    ///     POST /api/Payments              — create a Pending payment record for a lease
    ///     PUT  /api/Payments/{id}/record  — record receipt: sets AmountPaid, PaymentMethod,
    ///                                       ReceiptNumber. Status becomes Paid or Partial.
    ///                                       Sends PaymentRecorded notification to tenant.
    ///     GET  /api/Payments?status=Overdue — list overdue payments for follow-up
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public PaymentsController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/Payments
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? leaseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isTenant = User.IsInRole("Tenant");

                // Fail-closed: if the JWT claim is missing but the role is Tenant,
                // return Unauthorized rather than silently showing all payments.
                if (isTenant && userId == null)
                    return Unauthorized(new { message = "User identity claim is missing." });

                var query = _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .AsQueryable();

                // Tenants may only view their own payments
                if (isTenant)
                    query = query.Where(p => p.Lease.TenantId == userId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (leaseId.HasValue)
                {
                    query = query.Where(p => p.LeaseId == leaseId.Value);
                }

                var total    = await query.CountAsync();
                var payments = await query
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(p => new PaymentDto
                    {
                        PaymentId = p.PaymentId,
                        LeaseId = p.LeaseId,
                        TenantEmail = p.Lease.Tenant.Email ?? string.Empty,
                        UnitNumber = p.Lease.Unit.UnitNumber,
                        DueDate = p.DueDate,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                        Status = p.Status,
                        PaymentMethod = p.PaymentMethod,
                        ReceiptNumber = p.ReceiptNumber
                    })
                    .OrderByDescending(p => p.DueDate)
                    .ToListAsync();

                return Ok(new { total, page, pageSize, data = payments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving payments", error = ex.Message });
            }
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isTenant = User.IsInRole("Tenant");

                var payment = await _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .FirstOrDefaultAsync(p => p.PaymentId == id);

                if (payment == null)
                    return NotFound(new { message = $"Payment with ID {id} not found" });

                // Tenants may only view their own payments
                if (isTenant && payment.Lease.TenantId != userId)
                    return Forbid();

                return Ok(new PaymentDto
                {
                    PaymentId     = payment.PaymentId,
                    LeaseId       = payment.LeaseId,
                    TenantEmail   = payment.Lease.Tenant.Email ?? string.Empty,
                    UnitNumber    = payment.Lease.Unit.UnitNumber,
                    DueDate       = payment.DueDate,
                    AmountDue     = payment.AmountDue,
                    AmountPaid    = payment.AmountPaid,
                    PaymentDate   = payment.PaymentDate,
                    Status        = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    ReceiptNumber = payment.ReceiptNumber
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving payment", error = ex.Message });
            }
        }

        // GET: api/Payments/overdue
        [HttpGet("overdue")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> GetOverdue()
        {
            try
            {
                var overduePayments = await _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .Where(p => p.Status == "Overdue" ||
                               (p.Status == "Pending" && p.DueDate < DateTime.Now))
                    .Select(p => new PaymentDto
                    {
                        PaymentId = p.PaymentId,
                        LeaseId = p.LeaseId,
                        TenantEmail = p.Lease.Tenant.Email ?? string.Empty,
                        UnitNumber = p.Lease.Unit.UnitNumber,
                        DueDate = p.DueDate,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                        Status = p.Status,
                        PaymentMethod = p.PaymentMethod,
                        ReceiptNumber = p.ReceiptNumber
                    })
                    .OrderBy(p => p.DueDate)
                    .ToListAsync();

                return Ok(overduePayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving overdue payments", error = ex.Message });
            }
        }

        // POST: api/Payments
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Verify lease exists
                var lease = await _context.Leases.FindAsync(dto.LeaseId);
                if (lease == null)
                {
                    return BadRequest(new { message = $"Lease with ID {dto.LeaseId} not found" });
                }

                var payment = new Payment
                {
                    LeaseId = dto.LeaseId,
                    DueDate = dto.DueDate,
                    AmountDue = dto.AmountDue,
                    AmountPaid = 0,
                    Status = "Pending"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = payment.PaymentId }, new { paymentId = payment.PaymentId });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error creating payment", error = ex.Message });
            }
        }

        // PUT: api/Payments/5/record
        // Updates an existing payment by recording receipt details — PUT per REST semantics.
        [HttpPut("{id}/record")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var payment = await _context.Payments.FindAsync(id);

                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found" });
                }

                if (payment.Status == "Paid")
                {
                    return BadRequest(new { message = "Payment has already been recorded" });
                }

                payment.AmountPaid = dto.AmountPaid;
                payment.PaymentDate = DateTime.Now;
                payment.PaymentMethod = dto.PaymentMethod;
                payment.ReceiptNumber = dto.ReceiptNumber;
                payment.Status = dto.AmountPaid >= payment.AmountDue ? "Paid" : "Partial";

                await _context.SaveChangesAsync();

                // Notify the tenant their payment has been received
                var lease = await _context.Leases.FindAsync(payment.LeaseId);
                if (lease != null)
                    await _notificationService.PaymentRecordedAsync(lease.TenantId, dto.AmountPaid);

                return Ok(new
                {
                    message = "Payment recorded successfully",
                    status = payment.Status,
                    receiptNumber = payment.ReceiptNumber
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error recording payment", error = ex.Message });
            }
        }

        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);

                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found" });
                }

                if (payment.Status == "Paid")
                {
                    return BadRequest(new { message = "Cannot delete paid payments" });
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error deleting payment", error = ex.Message });
            }
        }
    }
}