namespace Northwind.Application.Common;

/// <summary>
/// A page of results from a paginated query. Carries the items, the page number
/// and size requested, and the total count so clients can render pagination UI.
/// </summary>
/// <typeparam name="T">The element type — typically a DTO or domain entity.</typeparam>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }

    /// <summary>Total number of pages, computed from TotalCount and PageSize.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}