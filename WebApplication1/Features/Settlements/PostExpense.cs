using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;

namespace WebApplication1.Features.Settlements;

// public class PostExpense : IEndpoint
// {
//     public void RegisterEndpoint(IEndpointRouteBuilder app)
//     {
//         app.MapPost("/groups/{groupId}/expenses", Handle)
//             .WithName("PostExpense")
//             .WithDescription("Creates a new expense within a group by a member")
//             .WithTags("Settlements")
//             .RequireAuthorization()
//             .WithOpenApi();
//     }
//
//     public static async Task<IResult> Handle(
//         [FromRoute] string groupId,
//         [FromBody] ExpenseRequestDto request,
//         AppDbContext dbContext,
//         ClaimsPrincipal currentUser,
//         HttpContext httpContext,
//         ILogger<PostExpense> logger,
//         CancellationToken cancellationToken)
//     {
//     }
// }