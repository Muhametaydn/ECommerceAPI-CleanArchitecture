using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Outbox;
using ECommerce.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ECommerce.UnitTests.Services;

public class OutboxProcessorJobTests
{
    private readonly Mock<IOutboxRepository> _outbox = new();
    private readonly Mock<IEventBus> _bus = new();

    [Fact]
    public async Task Publishes_known_event_with_its_payload_and_marks_processed()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var message = Message(new OrderStatusChangedIntegrationEvent(orderId, "ORD-1", userId, "Refunded"));
        Returns(message);
        OrderStatusChangedIntegrationEvent? published = null;
        _bus.Setup(b => b.PublishAsync(It.IsAny<OrderStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusChangedIntegrationEvent, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        await Job().ProcessAsync();

        // Payload JSON'dan geri kurulabilmeli; aksi halde e-postalar sessizce hiç gönderilmez.
        published.Should().NotBeNull();
        published!.OrderId.Should().Be(orderId);
        published.UserId.Should().Be(userId);
        published.NewStatus.Should().Be("Refunded");
        _outbox.Verify(o => o.MarkAsProcessedAsync(message.Id, It.IsAny<CancellationToken>()), Times.Once);
        _outbox.Verify(o => o.MarkAsFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_type_is_marked_failed_without_publishing()
    {
        var message = new OutboxMessage { Id = Guid.NewGuid(), Type = "Evil.Type, Attacker", Payload = "{}" };
        Returns(message);

        await Job().ProcessAsync();

        _outbox.Verify(o => o.MarkAsFailedAsync(message.Id, It.Is<string>(e => e.Contains("Bilinmeyen tip")), It.IsAny<CancellationToken>()), Times.Once);
        _bus.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Broker_failure_records_the_real_error_and_continues_with_next_message()
    {
        var failing = Message(new OrderStatusChangedIntegrationEvent(Guid.NewGuid(), "ORD-1", Guid.NewGuid(), "Shipped"));
        var next = Message(new OrderStatusChangedIntegrationEvent(Guid.NewGuid(), "ORD-2", Guid.NewGuid(), "Shipped"));
        Returns(failing, next);
        _bus.SetupSequence(b => b.PublishAsync(It.IsAny<OrderStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("RabbitMQ unreachable"))
            .Returns(Task.CompletedTask);

        await Job().ProcessAsync();

        // Regression: reflection çağrısı hatayı TargetInvocationException ile sarıyordu.
        _outbox.Verify(o => o.MarkAsFailedAsync(failing.Id, "RabbitMQ unreachable", It.IsAny<CancellationToken>()), Times.Once);
        _outbox.Verify(o => o.MarkAsProcessedAsync(next.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Empty_batch_does_nothing()
    {
        Returns();

        await Job().ProcessAsync();

        _bus.VerifyNoOtherCalls();
        _outbox.Verify(o => o.MarkAsProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private OutboxProcessorJob Job() => new(_outbox.Object, _bus.Object, NullLogger<OutboxProcessorJob>.Instance);

    private void Returns(params OutboxMessage[] messages)
        => _outbox.Setup(o => o.GetUnprocessedAsync(50, It.IsAny<CancellationToken>())).ReturnsAsync(messages);

    private static OutboxMessage Message<T>(T integrationEvent) => new()
    {
        Id = Guid.NewGuid(),
        Type = typeof(T).FullName!,
        Payload = JsonSerializer.Serialize(integrationEvent)
    };
}
