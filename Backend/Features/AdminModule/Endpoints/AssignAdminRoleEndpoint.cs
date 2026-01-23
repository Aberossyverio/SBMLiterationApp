using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.AdminModule.Endpoints;

public record AssignAdminRoleRequest(int UserId);

public class AssignAdminRoleEndpoint(UserManager<User> userManager)
    : Endpoint<AssignAdminRoleRequest>
{
    public override void Configure()
    {
        Post("{UserId}/assign");
        Group<AdminEndpointGroup>();
    }

    public override async Task HandleAsync(AssignAdminRoleRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.UserId.ToString());
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(ct, ct);
    }
}
