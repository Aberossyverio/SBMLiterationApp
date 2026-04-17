using Microsoft.AspNetCore.Identity;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.Auth;

public interface IPasskeyService
{
    Task<Result<CredentialCreateOptions>> BeginRegistrationAsync(string email, string? displayName = null);
    Task<Result<User>> CompleteRegistrationAsync(string email, AuthenticatorAttestationRawResponse attestationResponse);
    Task<Result<AssertionOptions>> BeginLoginAsync(string? email = null);
    Task<Result<User>> CompleteLoginAsync(string credentialId, AuthenticatorAssertionRawResponse assertionResponse);
}

public record CredentialCreateOptions(
    string Challenge,
    string RpId,
    string RpName,
    string UserId,
    string UserName,
    string UserDisplayName,
    int Timeout,
    string Attestation,
    string[] PubKeyCredParams,
    string AuthenticatorSelection
);

public record AssertionOptions(
    string Challenge,
    int Timeout,
    string RpId,
    string[]? AllowCredentials = null
);

public record AuthenticatorAttestationRawResponse(
    string Id,
    string RawId,
    string Type,
    AuthenticatorAttestationResponse Response,
    string? ClientDataJSON = null
);

public record AuthenticatorAttestationResponse(
    string AttestationObject,
    string ClientDataJSON
);

public record AuthenticatorAssertionRawResponse(
    string Id,
    string RawId,
    string Type,
    AuthenticatorAssertionResponse Response
);

public record AuthenticatorAssertionResponse(
    string AuthenticatorData,
    string ClientDataJSON,
    string Signature,
    string? UserHandle = null
);
