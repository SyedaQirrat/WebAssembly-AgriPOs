using AgriPosPoC.Core.Data;
using AgriPosPoC.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using System.Net.Http.Json;

namespace AgriPosPoC.Client
{
    public class SyncService : IAsyncDisposable
    {
        private readonly IDbContextFactory<OfflineDbContext> _offlineDbFactory;
        private readonly HttpClient _http;
        private readonly ILogger<SyncService> _logger;
        private HubConnection? _hubConnection;
        private Timer? _syncTimer;

        // Public properties for UI binding in MainLayout
        public string SyncStatus { get; private set; } = "Offline";
        public Color SyncColor { get; private set; } = Color.Error;
        public string SyncIcon { get; private set; } = Icons.Material.Filled.CloudOff;

        // Event for the layout to refresh
        public event EventHandler? SyncStatusChanged;

        public SyncService(IDbContextFactory<OfflineDbContext> offlineDbFactory, HttpClient http, ILogger<SyncService> logger, NavigationManager nav)
        {
            _offlineDbFactory = offlineDbFactory;
            _http = http;
            _logger = logger;

            // Start the background timer to attempt sync every 30 seconds
            _syncTimer = new Timer(async _ => await TrySyncAllAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

            // Initialize SignalR
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(nav.ToAbsoluteUri("/synchub")) // Assumes server is same origin
                .WithAutomaticReconnect()
                .Build();

            // Register the handler for server-to-client messages
            _hubConnection.On<string>("ReceiveDataUpdate", async (message) =>
            {
                _logger.LogInformation($"SignalR message received: {message}");
                await TrySyncProductsAsync();
                NotifyStatusChanged("Products Synced", Color.Success, Icons.Material.Filled.CloudDone);
            });

            // Start the SignalR connection
            //_ = StartSignalRConnectionAsync();
        }

        private async Task StartSignalRConnectionAsync()
        {
            try
            {
                if (_hubConnection is not null)
                {
                    await _hubConnection.StartAsync();
                    _logger.LogInformation("SignalR Connection Started.");
                    NotifyStatusChanged("Online", Color.Success, Icons.Material.Filled.CloudDone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"SignalR Connection Failed: {ex.Message}");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
        }

        private void NotifyStatusChanged(string status, Color color, string icon)
        {
            SyncStatus = status;
            SyncColor = color;
            SyncIcon = icon;
            SyncStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task TrySyncAllAsync()
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                await StartSignalRConnectionAsync();
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    _logger.LogWarning("Sync skipped: No server connection.");
                    NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
                    return;
                }
            }

            // *** THIS IS THE CORRECTED LINE ***
            NotifyStatusChanged("Syncing...", Color.Info, Icons.Material.Filled.Sync);

            await TrySyncInvoicesAsync();
            await TrySyncProductsAsync();
            NotifyStatusChanged("Online", Color.Success, Icons.Material.Filled.CloudDone);
        }

        // UPLOAD Invoices to Server
        public async Task TrySyncInvoicesAsync()
        {
            await using var db = await _offlineDbFactory.CreateDbContextAsync();

            var unsyncedInvoices = await db.Invoices
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
                var response = await _http.PostAsJsonAsync("api/sync/invoices", unsyncedInvoices);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Sync successful! Updating local status.");
                    foreach (var inv in unsyncedInvoices)
                    {
                        inv.IsSynced = true;
                    }
                    db.UpdateRange(unsyncedInvoices);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Sync failed (likely offline): {ex.Message}");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
        }

        // DOWNLOAD Products from Server
        public async Task TrySyncProductsAsync()
        {
            await using var db = await _offlineDbFactory.CreateDbContextAsync();
            _logger.LogInformation("Checking for product updates...");

            try
            {
                var serverProducts = await _http.GetFromJsonAsync<List<Product>>("api/sync/products");
                if (serverProducts == null || !serverProducts.Any()) return;

                var localProductIds = await db.Products.Select(p => p.Id).ToListAsync();

                var newProducts = serverProducts.Where(p => !localProductIds.Contains(p.Id)).ToList();

                if (newProducts.Any())
                {
                    _logger.LogInformation($"Found {newProducts.Count} new products from server.");
                    await db.Products.AddRangeAsync(newProducts);
                    await db.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("Local products are up-to-date.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Product sync failed (likely offline): {ex.Message}");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
        }

        // Clean up connections
        public async ValueTask DisposeAsync()
        {
            _syncTimer?.Dispose();
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}