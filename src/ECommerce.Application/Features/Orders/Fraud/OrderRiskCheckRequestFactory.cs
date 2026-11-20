using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.Fraud;

/// <summary>
/// Siparişten fraud servisine gidecek olayı üretir. Fraud servisi ana veritabanını okumadığı için
/// karar için gereken her şey buraya konur; kişisel veri ise en aza indirilir (KVKK).
/// </summary>
public static partial class OrderRiskCheckRequestFactory
{
    public const int MaxUserAgentLength = 512;

    public static OrderRiskCheckRequested Create(
        Order order,
        IReadOnlyCollection<Product> products,
        Address shippingAddress,
        UserRiskProfile? customer,
        string? clientIpAddress,
        string? userAgent,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(shippingAddress);

        var categories = products.ToDictionary(product => product.Id, product => product.CategoryId);
        var countryCode = ToCountryCode(shippingAddress.Country);

        return new OrderRiskCheckRequested
        {
            EventId = Guid.NewGuid(),
            OccurredAt = occurredAt,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Customer = new RiskCustomer
            {
                UserId = order.UserId,
                // Profil bulunamazsa hesap "şimdi açılmış" sayılır: bilinmeyen hesap güvenli varsayılmaz.
                AccountCreatedAt = customer is null ? occurredAt : AsUtc(customer.AccountCreatedAt),
                EmailDomain = EmailDomain(customer?.Email),
                EmailConfirmed = customer?.EmailConfirmed ?? false
            },
            Channel = new RiskChannel
            {
                IpAddress = clientIpAddress?.Trim() ?? string.Empty,
                UserAgent = Truncate(userAgent, MaxUserAgentLength)
            },
            ShippingAddress = new RiskShippingAddress
            {
                AddressId = shippingAddress.Id,
                Fingerprint = AddressFingerprint(shippingAddress, countryCode),
                City = shippingAddress.City.Trim(),
                District = shippingAddress.District.Trim(),
                PostalCode = shippingAddress.PostalCode.Trim(),
                CountryCode = countryCode
            },
            Items = order.OrderItems.Select(item => new RiskOrderItem
            {
                ProductId = item.ProductId,
                CategoryId = categories.GetValueOrDefault(item.ProductId),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToArray(),
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            Currency = "TRY",
            PaymentMethod = order.Payment?.Method switch
            {
                PaymentMethod.CreditCard => PaymentMethods.CreditCard,
                PaymentMethod.DebitCard => PaymentMethods.DebitCard,
                PaymentMethod.BankTransfer => PaymentMethods.BankTransfer,
                _ => string.Empty
            },
            CouponCode = order.CouponCode
        };
    }

    /// <summary>
    /// "Aynı adres birçok hesapta mı?" kontrolü için adresin kendisi yerine özeti gönderilir.
    /// Yazım farkları (büyük/küçük harf, Türkçe karakter, boşluk, noktalama) aynı özeti verir.
    /// </summary>
    public static string AddressFingerprint(Address address, string countryCode)
    {
        ArgumentNullException.ThrowIfNull(address);
        var canonical = string.Join('|',
            Normalize(address.AddressLine),
            Normalize(address.District),
            Normalize(address.City),
            Normalize(address.PostalCode),
            countryCode);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static string ToCountryCode(string? country)
    {
        var value = country?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return string.Empty;

        return Normalize(value) switch
        {
            "turkiye" or "turkey" or "tr" or "turkiye cumhuriyeti" => "TR",
            var code when code.Length == 2 && code.All(char.IsAsciiLetter) => code.ToUpperInvariant(),
            _ => string.Empty
        };
    }

    public static string EmailDomain(string? email)
    {
        var at = email?.LastIndexOf('@') ?? -1;
        return at < 0 || at == email!.Length - 1
            ? string.Empty
            : email[(at + 1)..].Trim().ToLowerInvariant();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.ToLower(CultureInfo.GetCultureInfo("tr-TR")))
        {
            builder.Append(ch switch
            {
                'ı' => 'i', 'ş' => 's', 'ğ' => 'g', 'ü' => 'u', 'ö' => 'o', 'ç' => 'c',
                _ => char.IsLetterOrDigit(ch) ? ch : ' '
            });
        }
        return Whitespace().Replace(builder.ToString(), " ").Trim();
    }

    private static DateTimeOffset AsUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : value.Kind).ToUniversalTime());

    private static string? Truncate(string? value, int length)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.Length <= length ? trimmed : trimmed[..length];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
