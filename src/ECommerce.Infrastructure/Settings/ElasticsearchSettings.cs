namespace ECommerce.Infrastructure.Settings;

public class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string ProductIndexName { get; set; } = "products";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
