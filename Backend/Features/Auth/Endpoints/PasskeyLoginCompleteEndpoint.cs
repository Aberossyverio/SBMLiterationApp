using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record PasskeyLoginCompleteRequest(
    string Id,
    string RawId,
    string Type,
    PasskeyAssertionResponse Response
);

public record PasskeyAssertionResponse(
    string AuthenticatorData,
    string ClientDataJSON,
    string Signature,
    string? UserHandle = null
);

public class PasskeyLoginCompleteRequestValidator : Validator<PasskeyLoginCompleteRequest>
{
    public PasskeyLoginCompleteRequestValidator()
    {
        RuleFor(x => x.RawId)
            .NotEmpty()
            .WithMessage("Credential ID is required");

        RuleFor(x => x.Response)
            .NotNull()
            .WithMessage("Assertion response is required");

        RuleFor(x => x.Response.Signature)
            .NotEmpty()
            .WithMessage("Signature is required");
    }
}

public class PasskeyLoginCompleteEndpoint(
    IPasskeyService passkeyService,
    UserManager<User> userManager,
    IJwtTokenService jwtTokenService) : Endpoint<PasskeyLoginCompleteRequest, PasskeyAuthResponse>
{

    public override void Configure()
    {
        Post("auth/passkey/login/complete");
        AllowAnonymous();
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(PasskeyLoginCompleteRequest req, CancellationToken ct)
    {
        var assertionResponse = new AuthenticatorAssertionRawResponse(
            Id: req.Id,
            RawId: req.RawId,
            Type: req.Type,
            Response: new AuthenticatorAssertionResponse(
                AuthenticatorData: req.Response.AuthenticatorData,
                ClientDataJSON: req.Response.ClientDataJSON,
                Signature: req.Response.Signature,
                UserHandle: req.Response.UserHandle
            )
        );

        var result = await passkeyService.CompleteLoginAsync(req.RawId, assertionResponse);

        if (result.IsFailure)
        {
            await Send.ResultAsync(TypedResults.BadRequest(new PasskeyAuthResponse
            {
                Success = false,
                Message = result.Error.Description ?? "Authentication failed"
            }));
            return;
        }

        var user = result.Value!;

        if (await userManager.IsLockedOutAsync(user))
        {
            await Send.ResultAsync(TypedResults.BadRequest(new PasskeyAuthResponse
            {
                Success = false,
                Message = "Account is locked. Please contact administrator."
            }));
            return;
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await jwtTokenService.GenerateTokensAsync(user, roles);

        await Send.OkAsync(new PasskeyAuthResponse
        {
            Success = true,
            Message = "Authentication successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, ct);
    }
}
