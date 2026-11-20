using ECommerce.Domain.Valueobjects;
using FluentAssertions;

namespace ECommerce.UnitTests.Entities;

public sealed class EmailTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "person@example.com", false)]
    [InlineData("person@example.com", null, false)]
    [InlineData("person@example.com", "person@example.com", true)]
    [InlineData(" Person@Example.com ", "person@example.com", true)]
    [InlineData("person@example.com", "other@example.com", false)]
    public void EqualityOperators_HandleNullAndCompareNormalizedValues(string? first, string? second, bool expected)
    {
        Email? left = first is null ? null : new Email(first);
        Email? right = second is null ? null : new Email(second);

        (left == right).Should().Be(expected);
        (right == left).Should().Be(expected);
        (left != right).Should().Be(!expected);
    }
}
