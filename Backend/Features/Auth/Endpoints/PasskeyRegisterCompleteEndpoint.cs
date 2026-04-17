using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record PasskeyRegisterCompleteRequest(
    string Email,
    string Id,
    string RawId,
    string Type,
    PasskeyAttestationResponse Response
);

public record PasskeyAttestationResponse(
    string AttestationObject,
    string ClientDataJSON
);

public class PasskeyRegisterCompleteRequestValidator : Validator<PasskeyRegisterCompleteRequest>
{
    public PasskeyRegisterCompleteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.RawId)
            .NotEmpty()
            .WithMessage("Credential ID is required");

        RuleFor(x => x.Response)
            .NotNull()
            .WithMessage("Attestation response is required");
    }
}

public class PasskeyRegisterCompleteEndpoint(
    IPasskeyService passkeyService,
    UserManager<User> userManager,
    IJwtTokenService jwtTokenService) : Endpoint<PasskeyRegisterCompleteRequest, PasskeyAuthResponse>
{
    public override void Configure()
    {
        Post("auth/passkey/register/complete");
        AllowAnonymous();
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(PasskeyRegisterCompleteRequest req, CancellationToken ct)
    {
        var attestationResponse = new AuthenticatorAttestationRawResponse(
            Id: req.Id,
            RawId: req.RawId,
            Type: req.Type,
            Response: new AuthenticatorAttestationResponse(
                AttestationObject: req.Response.AttestationObject,
                ClientDataJSON: req.Response.ClientDataJSON
            )
        );

        var result = await passkeyService.CompleteRegistrationAsync(req.Email, attestationResponse);

        if (result.IsFailure)
        {
            await Send.ResultAsync(TypedResults.BadRequest(new PasskeyAuthResponse
            {
                Success = false,
                Message = result.Error.Description ?? "Registration failed"
            }));
            return;
        }

        var user = result.Value!;
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await jwtTokenService.GenerateTokensAsync(user, roles);

        await Send.OkAsync(new PasskeyAuthResponse
        {
            Success = true,
            Message = "Passkey registered successfully",
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, ct);
    }
}

public class PasskeyAuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
