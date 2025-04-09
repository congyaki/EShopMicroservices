namespace Auth.API.Interfaces.Services
{
    public interface ITokenRevocationService
    {
        Task RevokeTokenAsync(string jti);
        Task<bool> IsTokenRevokedAsync(string jti);
    }
}
