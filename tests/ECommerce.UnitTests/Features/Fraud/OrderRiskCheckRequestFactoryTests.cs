using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Orders.Fraud;
using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Features.Fraud;

public class OrderRiskCheckRequestFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 30, 0, TimeSpan.Zero);
    private readonly Product _phone = new() { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = "Telefon", Price = 1000 };
    private readonly Product _case = new() { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = "Kılıf", Price = 100 };
    private readonly Address _address = new()
    {
        Id = Guid.NewGuid(), AddressLine = "Caferağa Mah. Moda Cad. No:12", District = "Kadıköy",
        City = "İstanbul", PostalCode = "34710", Country = "Türkiye"
    };

    [Fact]
    public void Carries_everything_the_fraud_service_needs()
    {
        var order = NewOrder();
        var created = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        var message = Create(order, new UserRiskProfile(created, "Ali.Veli@Outlook.COM", true), "203.0.113.7", "Mozilla/5.0");

        message.OrderId.Should().Be(order.Id);
        message.OrderNumber.Should().Be(order.OrderNumber);
        message.EventId.Should().NotBeEmpty();
        message.OccurredAt.Should().Be(Now);
        message.SchemaVersion.Should().Be(1);
        message.Customer.Should().BeEquivalentTo(new RiskCustomer
        {
            UserId = order.UserId, AccountCreatedAt = new DateTimeOffset(created), EmailDomain = "outlook.com", EmailConfirmed = true
        });
        message.Channel.IpAddress.Should().Be("203.0.113.7");
        message.Channel.UserAgent.Should().Be("Mozilla/5.0");
        message.ShippingAddress.CountryCode.Should().Be("TR");
        message.ShippingAddress.City.Should().Be("İstanbul");
        message.Items.Should().BeEquivalentTo(new[]
        {
            new RiskOrderItem { ProductId = _phone.Id, CategoryId = _phone.CategoryId, Quantity = 2, UnitPrice = 1000 },
            new RiskOrderItem { ProductId = _case.Id, CategoryId = _case.CategoryId, Quantity = 1, UnitPrice = 100 }
        });
        message.TotalAmount.Should().Be(2050);
        message.DiscountAmount.Should().Be(50);
        message.CouponCode.Should().Be("YAZ50");
        message.PaymentMethod.Should().Be(PaymentMethods.CreditCard);
        message.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Does_not_send_full_email_or_street_address()
    {
        var message = Create(NewOrder(), new UserRiskProfile(Now.UtcDateTime, "ali.veli@outlook.com", true), "1.1.1.1", null);

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        json.Should().NotContain("ali.veli").And.NotContain("Moda Cad");
        message.ShippingAddress.Fingerprint.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Address_fingerprint_ignores_case_turkish_letters_spacing_and_punctuation()
    {
        var variant = new Address
        {
            AddressLine = "  CAFERAGA mah   moda cad no 12 ", District = "KADIKOY", City = "istanbul",
            PostalCode = "34710", Country = "TR"
        };
        var other = new Address
        {
            AddressLine = "Caferağa Mah. Moda Cad. No:14", District = "Kadıköy", City = "İstanbul",
            PostalCode = "34710", Country = "Türkiye"
        };

        var original = OrderRiskCheckRequestFactory.AddressFingerprint(_address, "TR");

        OrderRiskCheckRequestFactory.AddressFingerprint(variant, "TR").Should().Be(original);
        OrderRiskCheckRequestFactory.AddressFingerprint(other, "TR").Should().NotBe(original);
    }

    [Theory]
    [InlineData("Türkiye", "TR")]
    [InlineData("TURKEY", "TR")]
    [InlineData("tr", "TR")]
    [InlineData("de", "DE")]
    [InlineData("Almanya", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Country_is_sent_as_an_iso_code(string? country, string expected)
        => OrderRiskCheckRequestFactory.ToCountryCode(country).Should().Be(expected);

    [Theory]
    [InlineData("a@b.com", "b.com")]
    [InlineData("weird@sub@Example.ORG", "example.org")]
    [InlineData("no-at-sign", "")]
    [InlineData("trailing@", "")]
    [InlineData(null, "")]
    public void Email_is_reduced_to_its_domain(string? email, string expected)
        => OrderRiskCheckRequestFactory.EmailDomain(email).Should().Be(expected);

    [Fact]
    public void Unknown_customer_profile_is_treated_as_a_brand_new_unconfirmed_account()
    {
        var message = Create(NewOrder(), customer: null, "1.1.1.1", null);

        message.Customer.AccountCreatedAt.Should().Be(Now);
        message.Customer.EmailConfirmed.Should().BeFalse();
        message.Customer.EmailDomain.Should().BeEmpty();
    }

    [Fact]
    public void User_agent_is_truncated_and_blank_values_are_dropped()
    {
        var longAgent = new string('x', 2000);

        Create(NewOrder(), null, null, longAgent).Channel.UserAgent.Should().HaveLength(OrderRiskCheckRequestFactory.MaxUserAgentLength);
        Create(NewOrder(), null, null, "   ").Channel.UserAgent.Should().BeNull();
        Create(NewOrder(), null, null, null).Channel.IpAddress.Should().BeEmpty();
    }

    private OrderRiskCheckRequested Create(Order order, UserRiskProfile? customer, string? ip, string? userAgent)
        => OrderRiskCheckRequestFactory.Create(order, [_phone, _case], _address, customer, ip, userAgent, Now);

    private Order NewOrder()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "ORD-20261120-TEST", UserId = Guid.NewGuid(),
            CouponCode = "YAZ50", DiscountAmount = 50
        };
        order.OrderItems.Add(new OrderItem { Id = Guid.NewGuid(), ProductId = _phone.Id, Quantity = 2, UnitPrice = 1000 });
        order.OrderItems.Add(new OrderItem { Id = Guid.NewGuid(), ProductId = _case.Id, Quantity = 1, UnitPrice = 100 });
        order.CalculateTotal();
        order.Payment = new Payment { Method = PaymentMethod.CreditCard };
        return order;
    }
}
