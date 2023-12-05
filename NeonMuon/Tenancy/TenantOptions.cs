namespace NeonMuon.Tenancy;

public class TenantOptions
{
    public required string Id { get; set; }
    public string ContentRoot { get; set; } = ".tenants/{0}";
    public string WebRoot { get; set; } = "wwwroot";
    public string Starter { get; set; } = nameof(Starter);
    public required string[] Urls { get; set; }
}
