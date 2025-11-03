// /Models/Invoice.cs

using System.ComponentModel.DataAnnotations;

namespace AgriPosPoC.Core.Models
{
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; }

        // Add "= string.Empty;" to fix the warning
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsSynced { get; set; }
    }
}