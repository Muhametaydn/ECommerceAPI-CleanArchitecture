using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Valueobjects
{
    public class Email : IEquatable<Email>
    {
        public string Value { get; }

        public Email(string value) 
        { 
            if(string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email boş olamaz.");

            if(!value.Contains("@") || !value.Contains("."))
                throw new ArgumentException("Geçersiz email formatı.");

            Value = value.Trim().ToLowerInvariant();
        }

        public bool Equals(Email? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as Email);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value;

        public static bool operator ==(Email? left, Email? right)
        {
            if(left == null && right == null) return true;
            if(left == null || right == null) return false;

            return left.Equals(right);

        }
        public static bool operator !=(Email? left, Email? right) => !(left == right);
        
        
        
    }
}
