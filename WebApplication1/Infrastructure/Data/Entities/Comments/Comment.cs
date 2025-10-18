using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Infrastructure.Data.Entities.Comments;

public class Comment
{
    [Key]
    public string Id { get; set; } = null!;
    
    [Required]
    public string TargetId { get; set; } = null!;
    
    [Required]
    public string TargetType { get; set; } = null!;
    
    [Required]
    public string UserId { get; set; } = null!;
    
    public string Content { get; set; } = null!;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
}