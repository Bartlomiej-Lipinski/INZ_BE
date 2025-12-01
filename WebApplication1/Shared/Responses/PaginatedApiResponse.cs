namespace WebApplication1.Shared.Responses;

public class PagedApiResponse<T> : ApiResponse<List<T>>
{
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}