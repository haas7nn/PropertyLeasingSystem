using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;

namespace PropertyManagement.API.Services
{
    /// <summary>
    /// Hourly background service that flags overdue payments automatically.
    ///
    /// Previously, overdue detection only ran when someone visited Payments/Index.
    /// This service runs independently so statuses are accurate even if no one
    /// visits the page — important for dashboard KPIs and the Reporting App.
    ///
    /// Uses IServiceScopeFactory to resolve the scoped DbContext from a singleton
    /// hosted service — direct injection of scoped services into singletons is
    /// not allowed by the ASP.NET Core DI container.
    /// </summary>
    public class OverduePaymentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverduePaymentService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public OverduePaymentService(IServiceScopeFactory scopeFactory, ILogger<OverduePaymentService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverduePaymentService started — runs every hour.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await FlagOverduePaymentsAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task FlagOverduePaymentsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var updated = await db.Payments
                    .Where(p => p.Status == "Pending" && p.DueDate < DateTime.UtcNow)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Overdue"), ct);
                if (updated > 0)
                    _logger.LogInformation("Flagged {N} payment(s) as Overdue.", updated);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log and continue — service will retry on the next interval.
                _logger.LogError(ex, "OverduePaymentService error during check.");
            }
        }
    }
}
