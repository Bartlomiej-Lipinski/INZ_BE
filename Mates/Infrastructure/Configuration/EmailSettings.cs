namespace Mates.Infrastructure.Configuration;

public class EmailSettings
{
    public string ApiKey { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public string SenderName { get; set; } = null!;
}