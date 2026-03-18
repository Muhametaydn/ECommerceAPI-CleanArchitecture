using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Valueobjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Tutar negatif olamaz.");

            if (string.IsNullOrEmpty(currency))
                throw new ArgumentNullException("Para birimi boş olamaz.");

            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }

        //iki moneyi topla
        public Money Add(Money other)
        {
            if(Currency != other.Currency)
                throw new InvalidOperationException("Farklı para birimleri toplanamaz.");

            return new Money(Amount + other.Amount, Currency);

        }

        //iki moneyi karsilastir
        public bool Equals(Money? other) { 
        if(other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj) => Equals(obj as Money);

        public override int GetHashCode() => HashCode.Combine(Amount, Currency);

        public override string ToString() => $"{Amount} {Currency}";

        public static bool operator == (Money? left, Money? right) 
        { 
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        public static bool operator != (Money? left, Money? right) => !(left == right);
    }
}
