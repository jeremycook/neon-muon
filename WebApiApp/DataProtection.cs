using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace WebApiApp;

public record DataProtectionSettings(string ConnectionString, string PemFile);

public class DataProtectionDbContext : DbContext, IDataProtectionKeyContext {
    public DataProtectionDbContext(DbContextOptions<DataProtectionDbContext> options) : base(options) { }
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

public static class DataProtectionExtensions {
    public static void AddDataProtection(this WebApplicationBuilder builder) {

        var settings = builder.Configuration.GetRequiredSection("DataProtection").Get<DataProtectionSettings>()!;

        // Read the certificate text
        string certText = File.ReadAllText(settings.PemFile);
        certText = Regex.Replace(certText, "^-.+", "", RegexOptions.Multiline);
        certText = Regex.Replace(certText, @"\s", "");
        var certBytes = Convert.FromBase64String(certText);
        var certificate = new X509Certificate2(certBytes, string.Empty);

        // Configure database storage
        var connectionString = settings.ConnectionString;
        {
            using var db = new DataProtectionDbContext(new DbContextOptionsBuilder<DataProtectionDbContext>()
                .UseSqlite(connectionString).Options);
            db.Database.EnsureCreated();
        }
        builder.Services
            .AddDbContextPool<DataProtectionDbContext>((services, builder) => builder.UseSqlite(connectionString));

        builder.Services
            .AddDataProtection()
            .ProtectKeysWithCertificate(certificate)
            .PersistKeysToDbContext<DataProtectionDbContext>();
    }
}
