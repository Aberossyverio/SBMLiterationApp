using FastEndpoints;
using PureTCOWebApp.Core;

namespace PureTCOWebApp.Features.Auth.Endpoints;

public record PasskeyLoginBeginRequest(string? Email = null);

public class PasskeyLoginBeginEndpoint(IPasskeyService passkeyService) : Endpoint<PasskeyLoginBeginRequest, ApiResponse<AssertionOptions>>
{

    public override void Configure()
    {
        Post("auth/passkey/login/begin");
        AllowAnonymous();
        Group<GlobalApiEndpointGroup>();
    }

    public override async Task HandleAsync(PasskeyLoginBeginRequest req, CancellationToken ct)
    {
        var result = await passkeyService.BeginLoginAsync(req.Email);

        if (result.IsFailure)
        {
            await Send.ResultAsync(TypedResults.BadRequest<ApiResponse<AssertionOptions>>(result));
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
