using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class Payment : Common.BaseEntity
    {
        public decimal Amount { get; set; }
        public Enums.PaymentMethod Method { get; set; }
        public Enums.PaymentStatus Status { get; set; } = Enums.PaymentStatus.Pending;
        public string? TransacionId { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;


        public void MarkAsCompleted(string transactonId)
        {
            if (Status != Enums.PaymentStatus.Pending)
                throw new InvalidOperationException("Sadece bekleyen ödemeler tamamlanabilir.");

            Status = Enums.PaymentStatus.Completed;
            TransacionId = transactonId;
            UpdateAt = DateTime.UtcNow;
        }

        public void MarkAsFailed()
        {
            Status = Enums.PaymentStatus.Failed;
            UpdateAt = DateTime.UtcNow;

        }


    }
}
