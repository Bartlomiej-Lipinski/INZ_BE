namespace WebApplication1.Infrastructure.Configuration;

public class EmailSettings
{
    public string ApiKey { get; init; } = null!;
    public string SenderEmail { get; set; } = null!;
    public string SenderName { get; set; } = null!;
}