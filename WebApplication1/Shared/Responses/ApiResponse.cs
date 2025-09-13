namespace WebApplication1.Shared.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? traceId = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message, TraceId = traceId };
    }

    public static ApiResponse<T> Fail(string message, string? traceId = null)
    {
        return new ApiResponse<T> { Success = false, Data = default, Message = message, TraceId = traceId };
    }
}