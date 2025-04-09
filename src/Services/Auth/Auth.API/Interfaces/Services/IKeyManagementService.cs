namespace Auth.API.Interfaces.Services
{
    public interface IKeyManagementService
    {
        byte[] GetCurrentSigningKey();
        void RotateKeys();
    }
}
