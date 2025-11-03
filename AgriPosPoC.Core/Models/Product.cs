// /Models/Product.cs

using System.ComponentModel.DataAnnotations;

namespace AgriPosPoC.Core.Models
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}