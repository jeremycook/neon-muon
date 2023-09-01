namespace NeonMS.Authentication;

public class ConnectionCredential
{
    public ConnectionCredential(string connection, string username, string password)
    {
        Connection = connection;
        Username = username;
        Password = password;
    }

    public string Connection { get; }
    public string Username { get; }
    public string Password { get; }
}