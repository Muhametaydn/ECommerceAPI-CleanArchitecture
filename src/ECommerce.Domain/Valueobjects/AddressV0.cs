using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Valueobjects
{
    public class AddressV0 : IEquatable<AddressV0>
    {
        public string AddressLine { get; }
        public string City {  get; }
        public string District { get; } //Bolge
        public string PostalCode { get; }
        public string Country { get; }

        public AddressV0(string addressLine, string city, string district, string postalCode,string countryt) {

            if (string.IsNullOrWhiteSpace(addressLine))
                throw new ArgumentException("Adres satırı boş olamaz.");

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("Şehir boş olamaz.");

            AddressLine = addressLine;
            City = city;
            District = district;
            PostalCode = postalCode;
            Country = countryt; 
        }

        public bool Equals(AddressV0? other)
        {
            if (other is null) return false;

            return AddressLine == other.AddressLine 
                && City == other.City 
                && District == other.District 
                && PostalCode == other.PostalCode 
                && Country == other.Country;      


        }

        public override bool Equals(object? obj) => Equals(obj as AddressV0);

        public override int GetHashCode() => HashCode.Combine(AddressLine, City, District, PostalCode, Country);

        public override string ToString() => $"{AddressLine},{City},{District},{PostalCode},{Country}";

        public static bool operator == (AddressV0? left, AddressV0? right)
        {
            if(left is null && right is null) return true;
            if(left is AddressV0 || right is null) return false;
            return left.Equals(right) ;
        }
       
        public static bool operator !=(AddressV0 left, AddressV0 right) => !(left == right) ;
        
        










    }
}
