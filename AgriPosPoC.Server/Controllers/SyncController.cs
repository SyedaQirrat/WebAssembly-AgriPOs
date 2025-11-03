// AgriPosPoC.Server/Controllers/SyncController.cs

using AgriPosPoC.Core.Data;
using AgriPosPoC.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly OnlineDbContext _onlineDb;
        private readonly ILogger<SyncController> _logger;

        public SyncController(OnlineDbContext onlineDb, ILogger<SyncController> logger)
        {
            _onlineDb = onlineDb;
            _logger = logger;
        }

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
                    clientInvoice.IsSynced = true; // Mark as synced
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
    }
}