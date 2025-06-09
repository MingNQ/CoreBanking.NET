namespace CoreBanking.API.Models;

/// <summary>
/// A Frame to request paginated data
/// </summary>
/// <param name="pageSize">Page Size</param>
/// <param name="pageIndex">Current Index</param>
public class PaginationRequest(int pageSize = 10, int pageIndex = 0)
{
    public int PageSize { get; set; } = pageSize;
    public int PageIndex { get; set; } = pageIndex;
}
