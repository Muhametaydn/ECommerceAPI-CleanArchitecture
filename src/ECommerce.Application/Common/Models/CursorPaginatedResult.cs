namespace ECommerce.Application.Common.Models;

/// <summary>
/// Cursor-based sayfalama sonucu.
///
/// Offset-based (Skip/Take) yerine cursor (imleç) kullanir.
/// Cursor, sonuncu ögenin benzersiz bir tanımlayicisidir (genelde Id veya CreatedAt).
///
/// Istemci ilk istekte cursor gondermez → ilk sayfa gelir.
/// Sonraki sayfa icin response'daki NextCursor degerini gonderir.
///
/// Ornek kullanim:
///   GET /api/v1/products?pageSize=10                     → ilk 10 urun
///   GET /api/v1/products?pageSize=10&cursor=abc123       → abc123'den sonraki 10 urun
/// </summary>
public class CursorPaginatedResult<T>
{
    /// <summary>Mevcut sayfadaki ogeler</summary>
    public List<T> Items { get; }

    /// <summary>Toplam kayit sayisi (opsiyonel — buyuk veri setlerinde atlanabilir)</summary>
    public int TotalCount { get; }

    /// <summary>Sonraki sayfayi getirmek icin kullanilacak cursor degeri (null ise son sayfa)</summary>
    public string? NextCursor { get; }

    /// <summary>Sonraki sayfa var mi?</summary>
    public bool HasNextPage { get; }

    /// <summary>Istenen sayfa boyutu</summary>
    public int PageSize { get; }

    public CursorPaginatedResult(List<T> items, int totalCount, string? nextCursor, bool hasNextPage, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        NextCursor = nextCursor;
        HasNextPage = hasNextPage;
        PageSize = pageSize;
    }
}
