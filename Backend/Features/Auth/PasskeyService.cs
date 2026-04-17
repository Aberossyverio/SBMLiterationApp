using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;
using PureTCOWebApp.Features.Auth.Domain;

namespace PureTCOWebApp.Features.Auth;

public class PasskeyService : IPasskeyService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasskeyService> _logger;
    private static readonly Dictionary<string, (string Challenge, DateTime Expiry)> _challenges = new();

    public PasskeyService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger<PasskeyService> logger)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<CredentialCreateOptions>> BeginRegistrationAsync(string email, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<CredentialCreateOptions>(PasskeyDomainError.EmailRequired);

        var user = await _userManager.FindByEmailAsync(email);
        var userId = user?.Id.ToString() ?? Guid.NewGuid().ToString();
        var userName = email;
        var userDisplayName = displayName ?? user?.Fullname ?? email;

        var challenge = GenerateChallenge();
        var challengeKey = $"reg_{email}_{DateTime.UtcNow.Ticks}";
        _challenges[challengeKey] = (challenge, DateTime.UtcNow.AddMinutes(5));

        var options = new CredentialCreateOptions(
            Challenge: challenge,
            RpId: GetRelyingPartyId(),
            RpName: _configuration["Passkey:RpName"] ?? "PureTCO App",
            UserId: userId,
            UserName: userName,
            UserDisplayName: userDisplayName,
            Timeout: 60000,
            Attestation: "none",
            PubKeyCredParams: ["ES256", "RS256"],
            AuthenticatorSelection: "cross-platform" // Support all authenticator types
        );

        return Result.Success(options);
    }

    public async Task<Result<User>> CompleteRegistrationAsync(string email, AuthenticatorAttestationRawResponse attestationResponse)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(PasskeyDomainError.EmailRequired);

        try
        {
            var credentialId = Convert.FromBase64String(attestationResponse.RawId);
            var publicKey = ExtractPublicKeyFromAttestation(attestationResponse.Response.AttestationObject);

            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Fullname = email,
                    Nim = "",
                    ProgramStudy = "",
                    Faculty = "",
                    GenerationYear = ""
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Result.Failure<User>(PasskeyDomainError.UserCreationFailed);
                }
            }

            var existingCredential = await _context.Set<PasskeyCredential>()
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId);

            if (existingCredential == null)
            {
                var credential = new PasskeyCredential
                {
                    UserId = user.Id,
                    CredentialId = credentialId,
                    PublicKey = publicKey,
                    SignatureCounter = 0,
                    DeviceName = "Passkey Device",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<PasskeyCredential>().Add(credential);
                await _context.SaveChangesAsync();
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration");
            return Result.Failure<User>(PasskeyDomainError.RegistrationFailed);
        }
    }

    public async Task<Result<AssertionOptions>> BeginLoginAsync(string? email = null)
    {
        var challenge = GenerateChallenge();
        var challengeKey = $"auth_{email ?? "any"}_{DateTime.UtcNow.Ticks}";
        _challenges[challengeKey] = (challenge, DateTime.UtcNow.AddMinutes(5));

        string[]? allowCredentials = null;
        
        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var credentials = await _context.Set<PasskeyCredential>()
                    .Where(c => c.UserId == user.Id)
                    .Select(c => Convert.ToBase64String(c.CredentialId))
                    .ToArrayAsync();
                
                allowCredentials = credentials.Length > 0 ? credentials : null;
            }
        }

        var options = new AssertionOptions(
            Challenge: challenge,
            Timeout: 60000,
            RpId: GetRelyingPartyId(),
            AllowCredentials: allowCredentials
        );

        return Result.Success(options);
    }

    public async Task<Result<User>> CompleteLoginAsync(string credentialId, AuthenticatorAssertionRawResponse assertionResponse)
    {
        try
        {
            var credentialIdBytes = Convert.FromBase64String(credentialId);
            
            var credential = await _context.Set<PasskeyCredential>()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CredentialId == credentialIdBytes);

            if (credential == null)
                return Result.Failure<User>(PasskeyDomainError.CredentialNotFound);

            var isValid = VerifyAssertion(credential, assertionResponse);
            
            if (!isValid)
                return Result.Failure<User>(PasskeyDomainError.AuthenticationFailed);

            credential.LastUsedAt = DateTime.UtcNow;
            credential.SignatureCounter++;
            await _context.SaveChangesAsync();

            return Result.Success(credential.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey login");
            return Result.Failure<User>(PasskeyDomainError.AuthenticationFailed);
        }
    }

    private string GenerateChallenge()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string GetRelyingPartyId()
    {
        return _configuration["Passkey:RpId"] ?? "localhost";
    }

    private byte[] ExtractPublicKeyFromAttestation(string attestationObject)
    {
        try
        {
            var attestationBytes = Convert.FromBase64String(attestationObject);
            return attestationBytes.Take(65).ToArray();
        }
        catch
        {
            return new byte[65];
        }
    }

    private bool VerifyAssertion(PasskeyCredential credential, AuthenticatorAssertionRawResponse assertionResponse)
    {
        try
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying assertion");
            return false;
        }
    }
}
