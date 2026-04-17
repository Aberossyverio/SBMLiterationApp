using PureTCOWebApp.Core.Models;

namespace PureTCOWebApp.Features.Auth;

public static class PasskeyDomainError
{
    public static readonly Error UserNotFound = new("Passkey.UserNotFound", "User not found");
    public static readonly Error EmailRequired = new("Passkey.EmailRequired", "Email is required");
    public static readonly Error InvalidEmail = new("Passkey.InvalidEmail", "Invalid email format");
    public static readonly Error CredentialNotFound = new("Passkey.CredentialNotFound", "Passkey credential not found");
    public static readonly Error InvalidCredential = new("Passkey.InvalidCredential", "Invalid passkey credential");
    public static readonly Error RegistrationFailed = new("Passkey.RegistrationFailed", "Failed to register passkey");
    public static readonly Error AuthenticationFailed = new("Passkey.AuthenticationFailed", "Passkey authentication failed");
    public static readonly Error UserCreationFailed = new("Passkey.UserCreationFailed", "Failed to create user account");
    public static readonly Error InvalidChallenge = new("Passkey.InvalidChallenge", "Invalid or expired challenge");
}
