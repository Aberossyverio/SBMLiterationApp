namespace PureTCOWebApp.Features.Auth.Domain;

public class PasskeyCredential
{
    #pragma warning disable 
    public PasskeyCredential()
    {
    }
    #pragma warning restore
    
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public uint SignatureCounter { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
