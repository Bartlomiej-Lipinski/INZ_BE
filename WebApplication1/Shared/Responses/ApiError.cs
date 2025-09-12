namespace WebApplication1.Shared.Responses;

public class ApiError
{
    public string Code { get; set; } = "ERROR";
    public string Message { get; set; } = "An error occurred.";
    public List<string>? Details { get; set; }
    public string? TraceId { get; set; }
}