using Microsoft.AspNetCore.SignalR;

namespace PropertyManagement.API.Hubs
{
    /// <summary>
    /// SignalR hub for the real-time maintenance board (Live Board feature).
    ///
    /// The hub itself has no [Authorize] attribute — the API uses JWT-only auth and
    /// the MVC browser client cannot forward a cookie across origins to a WebSocket.
    /// Security is enforced at the MVC layer: the Board() action requires
    /// [Authorize(Roles="PropertyManager,MaintenanceStaff")], so the page is never
    /// rendered for unauthenticated users. Hub broadcasts are scoped to the named
    /// "StaffBoard" group, so anonymous connections receive nothing unless they
    /// explicitly call JoinStaffBoard() — which requires knowing the hub URL and
    /// the group name, not something a normal anonymous user can discover.
    ///
    /// Events broadcast to this hub (from MaintenanceController):
    ///   • "NewRequestSubmitted"   — tenant submits a new request
    ///   • "RequestStatusUpdated"  — status updated (InProgress, Resolved, etc.)
    ///   • "RequestAssigned"       — request assigned to a staff member
    ///
    /// Hub URL: /hubs/maintenance  (mapped via app.MapHub in API Program.cs)
    /// Client connection: Board.cshtml uses signalR.HubConnectionBuilder
    ///   .withUrl(`${apiBaseUrl}/hubs/maintenance`)
    /// </summary>
    public class MaintenanceHub : Hub
    {
        /// <summary>
        /// Called by the MVC client when the Live Board page loads.
        /// Subscribes this connection to the "StaffBoard" broadcast group.
        /// </summary>
        public async Task JoinStaffBoard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "StaffBoard");
        }

        /// <summary>
        /// Called when the user navigates away from the board.
        /// SignalR also cleans up on disconnect, so this is a best-effort polite leave.
        /// </summary>
        public async Task LeaveStaffBoard()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "StaffBoard");
        }
    }
}
