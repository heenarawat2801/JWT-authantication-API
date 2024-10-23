namespace CustomerAPI
{
    public interface IRefreshTokenGenerator
    {
        string GenerateToken(string username);
    }
}
