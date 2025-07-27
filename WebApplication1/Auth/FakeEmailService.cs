namespace WebApplication1.Auth;

public class FakeEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"TO: {to}\nSUBJECT: {subject}\nBODY:\n{body}");
        return Task.CompletedTask;
    }
}