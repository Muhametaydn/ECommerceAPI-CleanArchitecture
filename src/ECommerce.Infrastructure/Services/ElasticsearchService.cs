using AutoMapper;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Elastic.Clients.Elasticsearch (8.x) ile ürün arama servisi.
/// Index adı: appsettings → Elasticsearch:ProductIndexName (varsayılan: "products").
/// </summary>
public class ElasticsearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private readonly IMapper _mapper;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(
        IOptions<ElasticsearchSettings> settings,
        IMapper mapper,
        ILogger<ElasticsearchService> logger)
    {
        var cfg = settings.Value;
        _indexName = cfg.ProductIndexName;
        _mapper = mapper;
        _logger = logger;

        var esSettings = new ElasticsearchClientSettings(new Uri(cfg.Uri))
            .DefaultIndex(_indexName);

        if (!string.IsNullOrEmpty(cfg.Username) && !string.IsNullOrEmpty(cfg.Password))
            esSettings = esSettings.Authentication(
                new Elastic.Transport.BasicAuthentication(cfg.Username, cfg.Password));

        _client = new ElasticsearchClient(esSettings);
    }

    public async Task<ProductSearchResult> SearchProductsAsync(
        string? query,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStock,
        string? sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var from = (pageNumber - 1) * pageSize;

        // ── Filter listesi oluştur ─────────────────────────────────────────────
        var filters = new List<Query>
        {
            // Her zaman sadece aktif ürünler
            new TermQuery(Infer.Field<ProductSearchDocument>(f => f.IsActive)) { Value = true }
        };

        if (categoryId.HasValue)
            filters.Add(new TermQuery(Infer.Field<ProductSearchDocument>(f => f.CategoryId))
                { Value = categoryId.Value.ToString() });

        if (minPrice.HasValue || maxPrice.HasValue)
        {
            var rangeQuery = new NumberRangeQuery(Infer.Field<ProductSearchDocument>(f => f.Price));
            if (minPrice.HasValue) rangeQuery.Gte = (double)minPrice.Value;
            if (maxPrice.HasValue) rangeQuery.Lte = (double)maxPrice.Value;
            filters.Add(rangeQuery);
        }

        if (inStock == true)
            filters.Add(new NumberRangeQuery(Infer.Field<ProductSearchDocument>(f => f.StockQuantity)) { Gt = 0 });

        // ── Bool query ────────────────────────────────────────────────────────
        Query mainQuery;
        if (!string.IsNullOrWhiteSpace(query))
        {
            mainQuery = new BoolQuery
            {
                Must =
                [
                    new MultiMatchQuery
                    {
                        Query = query,
                        Fields = new[]
                        {
                            Infer.Field<ProductSearchDocument>(f => f.Name, 3.0f),  // name^3 boost
                            Infer.Field<ProductSearchDocument>(f => f.Description),
                            Infer.Field<ProductSearchDocument>(f => f.SKU)
                        },
                        Type = TextQueryType.BestFields,
                        Fuzziness = new Fuzziness("AUTO")
                    }
                ],
                Filter = filters
            };
        }
        else
        {
            mainQuery = new BoolQuery { Filter = filters };
        }

        // ── Sort ──────────────────────────────────────────────────────────────
        var sortOrder = sortDescending ? SortOrder.Desc : SortOrder.Asc;
        Field sortField = sortBy?.ToLowerInvariant() switch
        {
            "price" => Infer.Field<ProductSearchDocument>(f => f.Price),
            "name"  => Infer.Field<ProductSearchDocument>(f => f.Name),
            "stock" => Infer.Field<ProductSearchDocument>(f => f.StockQuantity),
            _       => Infer.Field<ProductSearchDocument>(f => f.CreatedAt)
        };
        var sortOrderForField = sortBy?.ToLowerInvariant() == "name"
            ? SortOrder.Asc
            : (sortBy == null ? SortOrder.Desc : sortOrder);

        var sort = new List<SortOptions>
        {
            SortOptions.Field(sortField, new FieldSort { Order = sortOrderForField })
        };

        // ── Arama ─────────────────────────────────────────────────────────────
        var request = new SearchRequest<ProductSearchDocument>(_indexName)
        {
            From = from,
            Size = pageSize,
            Query = mainQuery,
            Sort = sort
        };

        var response = await _client.SearchAsync<ProductSearchDocument>(request, cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Elasticsearch search hatası: {Error}",
                response.ElasticsearchServerError?.ToString());
            return new ProductSearchResult { PageNumber = pageNumber, PageSize = pageSize };
        }

        var items = response.Documents
            .Select(d => _mapper.Map<ProductDTO>(d))
            .ToList();

        return new ProductSearchResult
        {
            Items = items,
            TotalCount = response.Total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task IndexProductAsync(
        ProductSearchDocument document, CancellationToken cancellationToken = default)
    {
        var request = new IndexRequest<ProductSearchDocument>(document, _indexName, document.Id.ToString());
        var response = await _client.IndexAsync(request, cancellationToken);

        if (!response.IsValidResponse)
            _logger.LogError("Elasticsearch index hatası (Id: {Id}): {Error}",
                document.Id, response.ElasticsearchServerError?.ToString());
    }

    public async Task DeleteProductFromIndexAsync(
        Guid productId, CancellationToken cancellationToken = default)
    {
        var request = new DeleteRequest(_indexName, productId.ToString());
        var response = await _client.DeleteAsync(request, cancellationToken);

        // Result enum: Elastic.Clients.Elasticsearch.Result (Created/Updated/Deleted/NotFound/NoOp)
        if (!response.IsValidResponse && response.Result != Result.NotFound)
            _logger.LogError("Elasticsearch delete hatası (Id: {Id}): {Error}",
                productId, response.ElasticsearchServerError?.ToString());
    }

    public async Task ReindexAllAsync(
        IEnumerable<ProductSearchDocument> documents, CancellationToken cancellationToken = default)
    {
        var docs = documents.ToList();
        if (docs.Count == 0) return;

        // Index yoksa ES otomatik oluşturur (auto-mapping) — explicit mapping gerekmez
        var bulkRequest = new BulkRequest(_indexName);
        var operations = docs.Select(doc =>
            (IBulkOperation)new BulkIndexOperation<ProductSearchDocument>(doc)
            {
                Id = doc.Id.ToString()
            }).ToList();

        bulkRequest.Operations = new BulkOperationsCollection(operations);

        var response = await _client.BulkAsync(bulkRequest, cancellationToken);

        if (response.Errors)
            _logger.LogError("Elasticsearch bulk reindex hatası: {ErrorCount} başarısız",
                response.ItemsWithErrors.Count());
        else
            _logger.LogInformation("Elasticsearch reindex tamamlandı: {Count} ürün", docs.Count);
    }
}
