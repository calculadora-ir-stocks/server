namespace Core.Clients.Auth0
{
    public interface IAuth0Client
    {
        Task<string> GetToken();
    }
}