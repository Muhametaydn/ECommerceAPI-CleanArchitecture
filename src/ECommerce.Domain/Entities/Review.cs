using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Review : Common.BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating {  get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public void SetRating(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating 1-5 arasında olmalı.");

            Rating = rating;
            UpdateAt = DateTime.UtcNow;
        }
    }
}
