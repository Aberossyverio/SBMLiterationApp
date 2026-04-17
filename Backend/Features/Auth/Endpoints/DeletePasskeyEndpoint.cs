using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record DeletePasskeyRequest(int Id);

public class DeletePasskeyRequestValidator : Validator<DeletePasskeyRequest>
{
    public DeletePasskeyRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid passkey ID");
    }
}

public class DeletePasskeyEndpoint(ApplicationDbContext context) : Endpoint<DeletePasskeyRequest, ApiResponse>
{

    public override void Configure()
    {
        Delete("auth/passkey/{id}");
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(DeletePasskeyRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var passkey = await context.PasskeyCredentials
            .FirstOrDefaultAsync(p => p.Id == req.Id && p.UserId == userIdInt, ct);

        if (passkey == null)
        {
            await Send.ResultAsync(TypedResults.NotFound<ApiResponse>(
                Result.Failure(PasskeyDomainError.CredentialNotFound)));
            return;
        }

        context.PasskeyCredentials.Remove(passkey);
        await context.SaveChangesAsync(ct);

        await Send.OkAsync(Result.Success("Passkey deleted successfully"), ct);
    }
}
