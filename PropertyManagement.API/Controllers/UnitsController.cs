using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Controllers
{
    /// <summary>
    /// API Controller: Units
    ///
    /// Manages rentable units within buildings. Write operations require Role C.
    /// The availability filter (?available=true) is used by the MVC lease create page
    /// to populate the unit dropdown with only bookable units.
    ///
    ///   GET    /api/Units                — list all units (optional ?buildingId, ?available filter)
    ///   GET    /api/Units/{id}           — single unit with full detail
    ///   POST   /api/Units                — create a unit in a building (Manager only)
    ///   PUT    /api/Units/{id}           — update unit details (Manager only)
    ///   DELETE /api/Units/{id}           — delete if no lease history (Manager only)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UnitsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Units
        [HttpGet]
        [Authorize]  // Authenticated users only — building/unit data includes rent amounts
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? buildingId)
        {
            try
            {
                var query = _context.Units
                    .Include(u => u.Building)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(u => u.AvailabilityStatus == status);
                }

                // Filter by building
                if (buildingId.HasValue)
                {
                    query = query.Where(u => u.BuildingId == buildingId.Value);
                }

                var units = await query
                    .Select(u => new UnitDto
                    {
                        UnitId = u.UnitId,
                        BuildingId = u.BuildingId,
                        BuildingName = u.Building.Name,
                        UnitNumber = u.UnitNumber,
                        Type = u.Type,
                        Bedrooms = u.Bedrooms,
                        Bathrooms = u.Bathrooms,
                        SizeInSqFt = u.SizeInSqFt,
                        MonthlyRent = u.MonthlyRent,
                        Amenities = u.Amenities,
                        AvailabilityStatus = u.AvailabilityStatus
                    })
                    .ToListAsync();

                return Ok(units);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving units", error = ex.Message });
            }
        }

        // GET: api/Units/5
        [HttpGet("{id}")]
        [Authorize]  // Authenticated users only
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var unit = await _context.Units
                    .Include(u => u.Building)
                    .Where(u => u.UnitId == id)
                    .Select(u => new UnitDto
                    {
                        UnitId = u.UnitId,
                        BuildingId = u.BuildingId,
                        BuildingName = u.Building.Name,
                        UnitNumber = u.UnitNumber,
                        Type = u.Type,
                        Bedrooms = u.Bedrooms,
                        Bathrooms = u.Bathrooms,
                        SizeInSqFt = u.SizeInSqFt,
                        MonthlyRent = u.MonthlyRent,
                        Amenities = u.Amenities,
                        AvailabilityStatus = u.AvailabilityStatus
                    })
                    .FirstOrDefaultAsync();

                if (unit == null)
                {
                    return NotFound(new { message = $"Unit with ID {id} not found" });
                }

                return Ok(unit);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving unit", error = ex.Message });
            }
        }

        // POST: api/Units
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Verify building exists
                var building = await _context.Buildings.FindAsync(dto.BuildingId);
                if (building == null)
                {
                    return BadRequest(new { message = $"Building with ID {dto.BuildingId} not found" });
                }

                var unit = new Unit
                {
                    BuildingId = dto.BuildingId,
                    UnitNumber = dto.UnitNumber,
                    Type = dto.Type,
                    Bedrooms = dto.Bedrooms,
                    Bathrooms = dto.Bathrooms,
                    SizeInSqFt = dto.SizeInSqFt,
                    MonthlyRent = dto.MonthlyRent,
                    Amenities = dto.Amenities,
                    AvailabilityStatus = "Available"
                };

                _context.Units.Add(unit);
                await _context.SaveChangesAsync();

                var unitDto = new UnitDto
                {
                    UnitId = unit.UnitId,
                    BuildingId = unit.BuildingId,
                    BuildingName = building.Name,
                    UnitNumber = unit.UnitNumber,
                    Type = unit.Type,
                    Bedrooms = unit.Bedrooms,
                    Bathrooms = unit.Bathrooms,
                    SizeInSqFt = unit.SizeInSqFt,
                    MonthlyRent = unit.MonthlyRent,
                    Amenities = unit.Amenities,
                    AvailabilityStatus = unit.AvailabilityStatus
                };

                return CreatedAtAction(nameof(GetById), new { id = unit.UnitId }, unitDto);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error creating unit", error = ex.Message });
            }
        }

        // PUT: api/Units/5
        [HttpPut("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var unit = await _context.Units.FindAsync(id);

                if (unit == null)
                {
                    return NotFound(new { message = $"Unit with ID {id} not found" });
                }

                unit.UnitNumber = dto.UnitNumber;
                unit.Type = dto.Type;
                unit.Bedrooms = dto.Bedrooms;
                unit.Bathrooms = dto.Bathrooms;
                unit.SizeInSqFt = dto.SizeInSqFt;
                unit.MonthlyRent = dto.MonthlyRent;
                unit.Amenities = dto.Amenities;
                unit.AvailabilityStatus = dto.AvailabilityStatus;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error updating unit", error = ex.Message });
            }
        }

        // DELETE: api/Units/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var unit = await _context.Units
                    .Include(u => u.LeaseHistory)
                    .FirstOrDefaultAsync(u => u.UnitId == id);

                if (unit == null)
                {
                    return NotFound(new { message = $"Unit with ID {id} not found" });
                }

                // Check if unit has active leases
                var activeLeases = unit.LeaseHistory.Any(l => l.Status == "Active");
                if (activeLeases)
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete unit with active leases",
                        suggestion = "Terminate leases first"
                    });
                }

                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error deleting unit", error = ex.Message });
            }
        }
    }
}