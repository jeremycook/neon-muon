namespace NeonMS.Authentication;

public class ConnectionCredential
{
    public ConnectionCredential(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; } = null!;
    public string Password { get; } = null!;
}