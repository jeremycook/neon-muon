using FileMod;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SqliteMod;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace WebApiApp;

public class DataProtectionDbContext : DbContext, IDataProtectionKeyContext {
    public DataProtectionDbContext(DbContextOptions<DataProtectionDbContext> options) : base(options) { }
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

public static class DataProtectionExtensions {
    public static void AddDataProtection(this WebApplicationBuilder builder, AppSettings settingsData, AppData appData) {

        // Read the certificate text
        string certText = settingsData.ReadAllText("dp.pem");
        certText = Regex.Replace(certText, "^-.+", "", RegexOptions.Multiline);
        certText = Regex.Replace(certText, @"\s", "");
        var certBytes = Convert.FromBase64String(certText);
        var certificate = new X509Certificate2(certBytes, string.Empty);

        // Configure database storage
        var connectionString = appData.GetConnectionString("dp.db", Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate);
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
