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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SyncService> _logger;
        private HubConnection? _hubConnection;
        private Timer? _syncTimer;
        private bool _isInitialized = false;

        public string SyncStatus { get; private set; } = "Offline";
        public Color SyncColor { get; private set; } = Color.Error;
        public string SyncIcon { get; private set; } = Icons.Material.Filled.CloudOff;
        public event EventHandler? SyncStatusChanged;

        public SyncService(IDbContextFactory<OfflineDbContext> offlineDbFactory,
                           IHttpClientFactory httpClientFactory,
                           ILogger<SyncService> logger,
                           NavigationManager nav)
        {
            _offlineDbFactory = offlineDbFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _syncTimer = new Timer(async _ => await TrySyncAllAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(nav.ToAbsoluteUri("/synchub"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("ReceiveDataUpdate", async (message) =>
            {
                _logger.LogInformation($"SignalR message received: {message}");
                await TrySyncProductsAsync();
                NotifyStatusChanged("Products Synced", Color.Success, Icons.Material.Filled.CloudDone);
            });
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await StartSignalRConnectionAsync();
                await TrySyncAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial sync.");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private async Task StartSignalRConnectionAsync()
        {
            try
            {
                if (_hubConnection is not null && _hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    NotifyStatusChanged("Online", Color.Success, Icons.Material.Filled.CloudDone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not start SignalR connection.");
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
            if (!_isInitialized)
            {
                _logger.LogWarning("Sync timer ticked before initialization. Skipping.");
                return;
            }

            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                await StartSignalRConnectionAsync();
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
                    return;
                }
            }

            NotifyStatusChanged("Syncing...", Color.Info, Icons.Material.Filled.Refresh);
            await TrySyncInvoicesAsync();
            await TrySyncProductsAsync();
            NotifyStatusChanged("Online", Color.Success, Icons.Material.Filled.CloudDone);
        }

        public async Task TrySyncInvoicesAsync()
        {
            await using var db = await _offlineDbFactory.CreateDbContextAsync();
            var unsyncedInvoices = await db.Invoices.Where(inv => !inv.IsSynced).ToListAsync();
            if (!unsyncedInvoices.Any()) return;
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ServerApi");
                var response = await httpClient.PostAsJsonAsync("api/sync/invoices", unsyncedInvoices);
                if (response.IsSuccessStatusCode)
                {
                    foreach (var inv in unsyncedInvoices) inv.IsSynced = true;
                    db.UpdateRange(unsyncedInvoices);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // --- FIX IS HERE ---
                _logger.LogWarning(ex, "Failed to sync invoices.");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
        }

        public async Task TrySyncProductsAsync()
        {
            await using var db = await _offlineDbFactory.CreateDbContextAsync();
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ServerApi");
                var serverProducts = await httpClient.GetFromJsonAsync<List<Product>>("api/sync/products");
                if (serverProducts == null || !serverProducts.Any()) return;
                var localProductIds = await db.Products.Select(p => p.Id).ToListAsync();
                var newProducts = serverProducts.Where(p => !localProductIds.Contains(p.Id)).ToList();
                if (newProducts.Any())
                {
                    await db.Products.AddRangeAsync(newProducts);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // --- FIX IS HERE ---
                _logger.LogWarning(ex, "Failed to sync products.");
                NotifyStatusChanged("Offline", Color.Error, Icons.Material.Filled.CloudOff);
            }
        }

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