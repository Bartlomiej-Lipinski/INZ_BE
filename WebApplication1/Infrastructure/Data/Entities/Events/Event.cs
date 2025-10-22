﻿using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Events;

public class Event
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    
    public bool IsAutoScheduled { get; set; }
    
    public DateTime? RangeStart { get; set; }
    public DateTime? RangeEnd { get; set; }
    public int? DurationMinutes { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public EventStatus Status { get; set; } = EventStatus.Planning;
    
    public DateTime CreatedAt { get; set; }
    
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<EventAvailability> Availabilities { get; set; } = new List<EventAvailability>();
    public ICollection<EventAvailabilityRange> AvailabilityRanges { get; set; } = new List<EventAvailabilityRange>();
    public ICollection<EventSuggestion> Suggestions { get; set; } = new List<EventSuggestion>();
}

public enum EventStatus
{
    Planning,
    SuggestionsReady,
    Confirmed,
    Cancelled
}