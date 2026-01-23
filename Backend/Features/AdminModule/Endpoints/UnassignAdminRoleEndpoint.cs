using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.AdminModule.Endpoints;

public record UnassignAdminRoleRequest(int UserId);

public class UnassignAdminRoleEndpoint(UserManager<User> userManager)
    : Endpoint<UnassignAdminRoleRequest>
{
    public override void Configure()
    {
        Delete("{UserId}/unassign");
        Group<AdminEndpointGroup>();
    }

    public override async Task HandleAsync(UnassignAdminRoleRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.UserId.ToString());
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await userManager.RemoveFromRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(ct, ct);
    }
}
