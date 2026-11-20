using ECommerce.Domain.Valueobjects;
using FluentAssertions;

namespace ECommerce.UnitTests.Entities;

public sealed class AddressV0Tests
{
    [Fact]
    public void Equal_values_should_compare_equal()
    {
        var first = new AddressV0("Atatürk Caddesi 1", "İstanbul", "Kadıköy", "34710", "Türkiye");
        var second = new AddressV0("Atatürk Caddesi 1", "İstanbul", "Kadıköy", "34710", "Türkiye");

        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.Equals(second).Should().BeTrue();
    }

    [Fact]
    public void Null_and_different_values_should_not_compare_equal()
    {
        var address = new AddressV0("Atatürk Caddesi 1", "İstanbul", "Kadıköy", "34710", "Türkiye");
        var different = new AddressV0("Atatürk Caddesi 2", "İstanbul", "Kadıköy", "34710", "Türkiye");
        AddressV0? missing = null;
        AddressV0? alsoMissing = null;

        (address == different).Should().BeFalse();
        (address == missing).Should().BeFalse();
        (missing == alsoMissing).Should().BeTrue();
    }
}
