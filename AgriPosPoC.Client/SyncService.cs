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
        private readonly IHttpClientFactory _httpClientFactory; // <-- CHANGED
        private readonly ILogger<SyncService> _logger;
        private HubConnection? _hubConnection;
        private Timer? _syncTimer;

        public string SyncStatus { get; private set; } = "Offline";
        public Color SyncColor { get; private set; } = Color.Error;
        public string SyncIcon { get; private set; } = Icons.Material.Filled.CloudOff;
        public event EventHandler? SyncStatusChanged;

        public SyncService(IDbContextFactory<OfflineDbContext> offlineDbFactory,
                           IHttpClientFactory httpClientFactory, // <-- CHANGED
                           ILogger<SyncService> logger,
                           NavigationManager nav)
        {
            _offlineDbFactory = offlineDbFactory;
            _httpClientFactory = httpClientFactory; // <-- CHANGED
            _logger = logger;
            _syncTimer = new Timer(async _ => await TrySyncAllAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
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

        private async Task StartSignalRConnectionAsync()
        {
            try
            {
                if (_hubConnection is not null)
                {
                    await _hubConnection.StartAsync();
                    NotifyStatusChanged("Online", Color.Success, Icons.Material.Filled.CloudDone);
                }
            }
            catch (Exception ex)
            {
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