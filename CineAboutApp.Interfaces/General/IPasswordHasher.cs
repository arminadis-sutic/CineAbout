namespace CineAboutApp.Interfaces.General
{
    public interface IPasswordHasher
    {
        (string Hash, string Salt) Hash(string password);

        bool Verify(string hash, string password, string salt);
    }
}
