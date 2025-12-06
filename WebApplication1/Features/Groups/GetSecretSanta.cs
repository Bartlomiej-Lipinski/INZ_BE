using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Groups;

public class GetSecretSanta : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/secret-santa", Handle)
            .WithName("GetSecretSanta")
            .WithDescription("Assigns Secret Santa pairs within a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetSecretSanta> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var groupUsers = await dbContext.GroupUsers
            .Include(gu => gu.User)
            .Where(gu => gu.GroupId == groupId && gu.AcceptanceStatus == AcceptanceStatus.Accepted)
            .ToListAsync(cancellationToken);

        if (groupUsers.Count < 2)
        {
            logger.LogWarning("Not enough users in group {GroupId} for Secret Santa. TraceId: {TraceId}",
                groupId, traceId);
            return Results.BadRequest(
                ApiResponse<string>.Fail("At least two users are required in the group for Secret Santa.", traceId));
        }

        var users = groupUsers.Select(gu => gu.User).ToList();
        
        var shuffled = users.OrderBy(_ => Random.Shared.Next()).ToList();
        var receivers = shuffled.Skip(1).Append(shuffled.First()).ToList();
        
        var userIds = users.Select(u => u.Id).ToList();
        
        var profilePictures = await dbContext.StoredFiles
            .AsNoTracking()
            .Where(f => userIds.Contains(f.UploadedById) && f.EntityType == EntityType.User)
            .GroupBy(f => f.UploadedById)
            .Select(g => g.OrderByDescending(x => x.UploadedAt).First())
            .ToDictionaryAsync(x => x.UploadedById, cancellationToken);

        var pairs = new List<SecretSantaResponseDto>();
        for (var i = 0; i < shuffled.Count; i++)
            pairs.Add(new SecretSantaResponseDto
            { 
                Giver = new UserResponseDto
                {
                    Id = shuffled[i].Id,
                    Name = shuffled[i].Name,
                    Surname = shuffled[i].Surname,
                    Username = shuffled[i].UserName,
                    ProfilePicture = profilePictures.TryGetValue(shuffled[i].Id, out var photo)
                        ? new ProfilePictureResponseDto
                        {
                            Url = photo.Url,
                            FileName = photo.FileName,
                            ContentType = photo.ContentType,
                            Size = photo.Size
                        }
                        : null
                }, 
                Receiver = new UserResponseDto
                {
                    Id = receivers[i].Id,
                    Name = receivers[i].Name,
                    Surname = receivers[i].Surname,
                    Username = receivers[i].UserName,
                    ProfilePicture = profilePictures.TryGetValue(receivers[i].Id, out var receiverPhoto)
                        ? new ProfilePictureResponseDto
                        {
                            Url = receiverPhoto.Url,
                            FileName = receiverPhoto.FileName,
                            ContentType = receiverPhoto.ContentType,
                            Size = receiverPhoto.Size
                        }
                        : null
                }
            }
        );

        logger.LogInformation("Secret Santa pairs assigned for GroupId: {GroupId}. TraceId: {TraceId}",
            groupId, traceId);

        return Results.Ok(
            ApiResponse<List<SecretSantaResponseDto>>.Ok(pairs, "Secret Santa pairs assigned successfully",
                traceId));
    }
}