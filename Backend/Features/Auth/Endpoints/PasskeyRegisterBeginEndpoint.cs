using FastEndpoints;
using FluentValidation;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record PasskeyRegisterBeginRequest(string Email, string? DisplayName = null);

public class PasskeyRegisterBeginRequestValidator : Validator<PasskeyRegisterBeginRequest>
{
    public PasskeyRegisterBeginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}

public class PasskeyRegisterBeginEndpoint(IPasskeyService passkeyService) : Endpoint<PasskeyRegisterBeginRequest, ApiResponse<CredentialCreateOptions>>
{

    public override void Configure()
    {
        Post("auth/passkey/register/begin");
        AllowAnonymous();
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(PasskeyRegisterBeginRequest req, CancellationToken ct)
    {
        var result = await passkeyService.BeginRegistrationAsync(req.Email, req.DisplayName);

        if (result.IsFailure)
        {
            await Send.ResultAsync(TypedResults.BadRequest<ApiResponse<CredentialCreateOptions>>(result));
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
