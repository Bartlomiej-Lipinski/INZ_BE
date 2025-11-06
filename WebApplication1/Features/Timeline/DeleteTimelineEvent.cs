using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;

namespace WebApplication1.Features.Timeline;

// public class DeleteTimelineEvent : IEndpoint
// {
//     public void RegisterEndpoint(IEndpointRouteBuilder app)
//     {
//         app.MapDelete("/groups/{groupId}/timeline/{eventId}", Handle)
//             .WithName("DeleteTimelineEvent")
//             .WithDescription("Deletes a timeline event in a specific group")
//             .WithTags("Timeline")
//             .RequireAuthorization();
//     }
//
//     public static async Task<IResult> Handle(
//         [FromRoute] string groupId,
//         [FromRoute] string eventId,
//         AppDbContext dbContext,
//         ClaimsPrincipal currentUser,
//         HttpContext httpContext,
//         ILogger<DeleteTimelineEvent> logger,
//         CancellationToken cancellationToken)
//     {
//         
//     }
// }