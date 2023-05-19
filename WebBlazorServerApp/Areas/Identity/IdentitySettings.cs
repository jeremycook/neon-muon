namespace WebBlazorServerApp.Areas.Identity;

public class IdentitySettings {
    public string ConnectionString { get; set; } = null!;
    public string SiteName { get; set; } = "TODO: Site Name";
    public string FromAddress { get; set; } = "identity@localhost";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
}
