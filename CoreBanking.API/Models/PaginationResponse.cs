namespace CoreBanking.API.Models;

/// <summary>
/// A Frame to return list of items(Entity)
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <param name="pageIndex">Current Page</param>
/// <param name="pageSize">Page Size</param>
/// <param name="count">Total Count</param>
/// <param name="items">List items</param>
public class PaginationResponse<TEntity>(int pageIndex, int pageSize, long count, IEnumerable<TEntity> items)
{
    public int PageIndex => pageIndex;
    public int PageSize => pageSize;
    public long Count => count;
    public IEnumerable<TEntity> Items => items;
}
