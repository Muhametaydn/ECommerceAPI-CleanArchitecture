using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ECommerce.Contracts.Fraud;
using FluentAssertions;

namespace ECommerce.UnitTests.Features.Fraud;

/// <summary>
/// Fraud servisiyle sözleşme testi. Örnek dosyalar ECommerceFraud/contracts/samples'tan kopyalanır ve
/// iki repoda da aynı testlerle doğrulanır. Bir alan bu tarafta yeniden adlandırılırsa veya silinirse
/// mesajlar sessizce kaybolmak yerine bu test kırılır.
/// </summary>
public class FraudContractTests
{
    // MassTransit'in kablodaki biçimi: camelCase. Disallow: örnekte olup sınıfta olmayan alanı yakalar.
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static TheoryData<string, Type> Samples => new()
    {
        { "order-risk-check-requested.v1.json", typeof(OrderRiskCheckRequested) },
        { "order-risk-assessed.v1.json", typeof(OrderRiskAssessed) },
        { "order-risk-reviewed.v1.json", typeof(OrderRiskReviewed) }
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void Sample_round_trips_with_the_same_fields(string file, Type contract)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ContractSamples", "Fraud", file));

        var message = JsonSerializer.Deserialize(json, contract, Wire);
        var written = JsonSerializer.SerializeToNode(message, contract, Wire)!;

        Paths(written).Should().BeEquivalentTo(Paths(JsonNode.Parse(json)!), "{0} örneği ile sınıf aynı alanlara sahip olmalı", file);
    }

    [Fact]
    public void Contracts_use_the_shared_namespace()
    {
        foreach (var type in new[] { typeof(OrderRiskCheckRequested), typeof(OrderRiskAssessed), typeof(OrderRiskReviewed) })
            type.FullName.Should().Be($"ECommerce.Contracts.Fraud.{type.Name}");
    }

    [Fact]
    public void Outbox_payload_survives_the_outbox_job_round_trip()
    {
        // CreateOrder varsayılan ayarlarla yazar, OutboxProcessorJob varsayılan ayarlarla okur.
        var sample = JsonSerializer.Deserialize<OrderRiskCheckRequested>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ContractSamples", "Fraud", "order-risk-check-requested.v1.json")), Wire)!;

        var restored = JsonSerializer.Deserialize<OrderRiskCheckRequested>(JsonSerializer.Serialize(sample));

        restored.Should().BeEquivalentTo(sample);
    }

    private static HashSet<string> Paths(JsonNode node, string prefix = "")
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        if (node is JsonObject obj)
        {
            foreach (var (key, value) in obj)
            {
                paths.Add($"{prefix}.{key}");
                if (value is not null)
                    paths.UnionWith(Paths(value, $"{prefix}.{key}"));
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array.Where(item => item is not null))
                paths.UnionWith(Paths(item!, $"{prefix}[]"));
        }
        return paths;
    }
}
