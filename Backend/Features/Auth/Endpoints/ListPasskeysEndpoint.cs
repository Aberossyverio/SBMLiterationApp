using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record PasskeyListResponse(
    int Id,
    string DeviceName,
    DateTime CreatedAt,
    DateTime? LastUsedAt
);

public class ListPasskeysEndpoint(ApplicationDbContext context) : EndpointWithoutRequest<ApiResponse<List<PasskeyListResponse>>>
{

    public override void Configure()
    {
        Get("auth/passkey/list");
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var passkeys = await context.PasskeyCredentials
            .Where(p => p.UserId == userIdInt)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PasskeyListResponse(
                p.Id,
                p.DeviceName ?? "Unknown Device",
                p.CreatedAt,
                p.LastUsedAt
            ))
            .ToListAsync(ct);

        await Send.OkAsync(Result.Success(passkeys), ct);
    }
}
