namespace Mates.Infrastructure.Data.Entities;

public class LoginAttempt
{
    public int Id { get; init; }
    
    public string Email { get; set; } = null!;
    public string? IpAddress { get; set; }
    public DateTime AttemptTime { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime CreatedAt { get; set; }
}