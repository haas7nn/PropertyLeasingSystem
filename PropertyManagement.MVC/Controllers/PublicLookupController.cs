using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    /// <summary>
    /// MVC Controller: Public Maintenance Request Lookup
    ///
    /// This controller demonstrates the project's HttpClient architectural requirement:
    /// it is the ONLY MVC controller that fetches data by calling the Web API via HttpClient.
    /// All other MVC controllers use EF Core directly via the shared ApplicationDbContext.
    ///
    /// Why HttpClient here?
    ///   The lookup endpoint (/api/Maintenance/lookup) is intentionally public (AllowAnonymous)
    ///   on the API side. This allows unauthenticated tenants to track their request without
    ///   creating an account or logging in — a common requirement in service portals.
    ///   Using HttpClient here also satisfies the brief's requirement that the MVC app
    ///   communicates with the API for at least one feature.
    ///
    /// Role A (Tenant) — primary user of this page:
    ///   1. Opens /PublicLookup without logging in.
    ///   2. Enters their Ticket Number (e.g., MNT-260514-02) and registered phone number.
    ///   3. The MVC controller calls GET /api/Maintenance/lookup via MaintenanceApiService.
    ///   4. Returns current status, assigned staff name, and resolution notes — read-only.
    ///
    ///   GET  /PublicLookup         — renders the lookup form
    ///   POST /PublicLookup/Search  — calls the API; renders Result view or error
    /// </summary>
    public class PublicLookupController : Controller
    {
        private readonly MaintenanceApiService _maintenanceService;

        public PublicLookupController(MaintenanceApiService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        /// <summary>Renders the ticket lookup form. No authentication required.</summary>
        public IActionResult Index() => View();

        /// <summary>
        /// Calls the API's public lookup endpoint and renders the result.
        /// Returns the form with an error message if the ticket/phone combination is not found.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string ticketNumber, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError("", "Both ticket number and phone number are required.");
                return View("Index");
            }

            // Calls GET /api/Maintenance/lookup via HttpClient (the only API call in the MVC app)
            var result = await _maintenanceService.LookupMaintenanceRequest(ticketNumber.Trim(), phoneNumber.Trim());

            if (result == null)
            {
                ViewBag.Error = "No matching request found. Please check your ticket number and the phone number registered on your account.";
                return View("Index");
            }

            return View("Result", result);
        }
    }
}
