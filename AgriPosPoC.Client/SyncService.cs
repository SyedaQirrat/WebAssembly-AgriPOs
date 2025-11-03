using AgriPosPoC.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace AgriPosPoC.Client
{
    public class SyncService
    {
        private readonly OfflineDbContext _offlineDb;
        private readonly HttpClient _http;
        private readonly ILogger<SyncService> _logger;

        public SyncService(OfflineDbContext offlineDb, HttpClient http, ILogger<SyncService> logger)
        {
            _offlineDb = offlineDb;
            _http = http;
            _logger = logger;
        }

        public async Task TrySyncInvoicesAsync()
        {
            _logger.LogInformation("Checking for unsynced invoices.");

            var unsyncedInvoices = await _offlineDb.Invoices
                                       .Where(inv => !inv.IsSynced)
                                       .ToListAsync();

            if (!unsyncedInvoices.Any())
            {
                _logger.LogInformation("No invoices to sync.");
                return;
            }

            _logger.LogInformation($"Found {unsyncedInvoices.Count} invoices to sync.");

            try
            {
                // This calls the API: "https://localhost:YYYY/api/sync/invoices"
                var response = await _http.PostAsJsonAsync("api/sync/invoices", unsyncedInvoices);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Sync successful! Updating local status.");
                    foreach (var inv in unsyncedInvoices)
                    {
                        inv.IsSynced = true;
                    }
                    _offlineDb.UpdateRange(unsyncedInvoices);
                    await _offlineDb.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning($"Sync failed with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Sync failed (likely offline): {ex.Message}");
            }
        }
    }
}