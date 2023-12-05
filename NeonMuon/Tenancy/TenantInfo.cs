namespace NeonMuon.Tenancy;

public class TenantInfo
{
    public required string Id { get; set; }
    public required string[] Urls { get; set; }

    public required string ContentRoot { get; set; }
    public required string EnvironmentName { get; set; }
    public required string WebRoot { get; set; }
}
