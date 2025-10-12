using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Infrastructure.Data.Entities;

public class TwoFactorCode
{
    public int Id { get; init; }
        
    [Required]
    public string UserId { get; set; }
        
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public DateTime ExpiresAt { get; set; }
        
    public bool IsUsed { get; set; }
        
    public string? IpAddress { get; set; }
        
    public string? UserAgent { get; set; }
}