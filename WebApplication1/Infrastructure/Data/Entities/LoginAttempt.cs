namespace WebApplication1.Infrastructure.Data.Entities;

public class LoginAttempt
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string IpAddress { get; set; }
    public DateTime AttemptTime { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime CreatedAt { get; set; }
}