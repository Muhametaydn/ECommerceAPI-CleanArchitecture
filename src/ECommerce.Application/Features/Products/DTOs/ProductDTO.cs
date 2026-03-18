using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Application.Features.Products.DTOs
          
{
    public class ProductDTO
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
        public string SKU { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
