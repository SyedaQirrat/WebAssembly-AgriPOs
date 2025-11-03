// /Controllers/SyncController.cs

using AgriPosPoC.Core.Data;
using AgriPosPoC.Core.Models;
using AgriPosPoC.Server.Hubs; // Import Hub
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // Import SignalR
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly OnlineDbContext _onlineDb;
        private readonly ILogger<SyncController> _logger;
        private readonly IHubContext<SyncHub> _hubContext; // Inject Hub

        public SyncController(OnlineDbContext onlineDb, ILogger<SyncController> logger, IHubContext<SyncHub> hubContext)
        {
            _onlineDb = onlineDb;
            _logger = logger;
            _hubContext = hubContext;
        }

        // POST: api/sync/invoices (for uploading from client)
        [HttpPost("invoices")]
        public async Task<IActionResult> SyncInvoices([FromBody] List<Invoice> invoicesFromClient)
        {
            if (invoicesFromClient == null || !invoicesFromClient.Any())
            {
                return BadRequest("No invoices to sync.");
            }

            _logger.LogInformation($"Receiving {invoicesFromClient.Count} invoices to sync.");

            foreach (var clientInvoice in invoicesFromClient)
            {
                var existing = await _onlineDb.Invoices.FindAsync(clientInvoice.Id);
                if (existing == null)
                {
                    clientInvoice.IsSynced = true;
                    _onlineDb.Invoices.Add(clientInvoice);
                }
                else
                {
                    existing.InvoiceNumber = clientInvoice.InvoiceNumber;
                    existing.InvoiceDate = clientInvoice.InvoiceDate;
                    existing.Amount = clientInvoice.Amount;
                    existing.IsSynced = true;
                    _onlineDb.Invoices.Update(existing);
                }
            }

            await _onlineDb.SaveChangesAsync();
            _logger.LogInformation("Sync complete. Invoices saved to online DB.");
            return Ok();
        }

        // GET: api/sync/products (for downloading from server)
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _onlineDb.Products.ToListAsync();
            return Ok(products);
        }

        // POST: api/sync/products (Example: for Head Office to add a new product)
        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            product.Id = Guid.NewGuid();
            _onlineDb.Products.Add(product);
            await _onlineDb.SaveChangesAsync();

            // Notify all connected clients that new product data is available
            await _hubContext.Clients.All.SendAsync("ReceiveDataUpdate", "Products updated");

            _logger.LogInformation($"New product added and clients notified.");
            return Ok(product);
        }
    }
}