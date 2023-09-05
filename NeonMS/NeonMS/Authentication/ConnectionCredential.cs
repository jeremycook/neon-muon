namespace NeonMS.Authentication;

public class CredentialSetting
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Role { get; set; } = null!;
}

public class ConnectionCredential
{
    public ConnectionCredential(string connection, string username, string password, string? role = null)
    {
        Connection = connection;
        Username = username;
        Password = password;
        Role = role;
    }

    public string Connection { get; }
    public string Username { get; }
    public string Password { get; }
    public string? Role { get; }
}