namespace Auth.API.Interfaces.Services
{
    public interface ITwoFactorService
    {
        string GenerateTwoFactorCode(string username);
        bool ValidateTwoFactorCode(string username, string code);
    }
}
