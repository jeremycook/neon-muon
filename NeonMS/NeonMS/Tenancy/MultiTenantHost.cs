using System.Reflection;
using System.Runtime.InteropServices;

namespace NeonMS.Tenancy;

public static class MultiTenantHost
{
    public static async Task RunAsync<TProgram>(string[] args) where TProgram : class
    {
        string? applicationName =
            Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ??
            typeof(TProgram).Assembly.GetName().Name;

        string envName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            "Production";
        bool isDevelopment = envName == "Development";

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);
        if (isDevelopment)
        {
            configurationBuilder.AddUserSecrets<TProgram>();
        }
        IConfigurationRoot configuration = configurationBuilder
            .Build();

        Log.Factory = LoggerFactory.Create(options => options
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddConsole());

        TenantOptions[] tenantsOptions = configuration
            .GetRequiredSection("Tenants")
            .Get<TenantOptions[]>()!;

        // Tenant post configuration
        foreach (var tenant in tenantsOptions)
        {
            if (tenant.Urls.Length == 0)
                throw new InvalidOperationException($"The {tenant.Id} tenant does not bind to any URLs. It must bind to at least one URL.");

            if (string.IsNullOrWhiteSpace(tenant.Id))
                tenant.Id = tenant.Urls[0];

            if (string.IsNullOrWhiteSpace(tenant.Starter))
                tenant.Starter = typeof(Starter).FullName!; // Name of the default startup class
        }

        var duplicateIds = tenantsOptions.GroupBy(o => o.Id).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateIds.Any())
            throw new InvalidOperationException("Every tenant ID must be unique. These are not unique: " + duplicateIds);

        var duplicateUrls = tenantsOptions.SelectMany(o => o.Urls).GroupBy(o => o).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateUrls.Any())
            throw new InvalidOperationException("A tenant URL can only be bound once. These URLs are reused: " + duplicateUrls);

        //TenantOptions[] tenantsOptions = Enumerable.Range(1, 1000)
        //    .Select(i => new TenantOptions
        //    {
        //        Id = "Tenant" + i,
        //        Startup = nameof(BasicTenant),
        //        Urls = new[] { "https://localhost:" + (7000 + i) },
        //    })
        //    .ToArray();

        Type[] starterClasses = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.ExportedTypes)
            .Where(t =>
                t.Name == nameof(Starter) ||
                t.GetCustomAttribute<StarterAttribute>() is not null
            )
            .ToArray();

        Dictionary<string, Type> starterLookup = starterClasses
            .ToDictionary(t => t.FullName + ", " + t.Assembly.GetName().Name);
        foreach (var group in starterClasses
            .GroupBy(t => t.Name + ", " + t.Assembly.GetName().Name)
            .Where(g => g.Count() == 1))
        {
            starterLookup.Add(group.Key, group.First());
        }
        foreach (var group in starterClasses
            .GroupBy(t => t.FullName!)
            .Where(g => g.Count() == 1))
        {
            starterLookup.Add(group.Key, group.First());
        }
        foreach (var group in starterClasses
            .GroupBy(t => t.Name)
            .Where(g => g.Count() == 1))
        {
            starterLookup.Add(group.Key, group.First());
        }

        var cancellationTokenSource = new CancellationTokenSource();
        PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
        {
            cancellationTokenSource.Cancel();
        });

        var tenantsTasks = new Task[tenantsOptions.Length];
        for (int i = 0; i < tenantsOptions.Length; i++)
        {
            var tenantOptions = tenantsOptions[i];

            if (!starterLookup.TryGetValue(tenantOptions.Starter, out var starterClass))
            {
                throw new NullReferenceException("Starter type not found: " + tenantOptions.Starter);
            }

            var mainMethod = starterClass
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(Starter.Main));

            var tenant = new TenantInfo
            {
                ContentRoot = Path.GetFullPath(string.Format(tenantOptions.ContentRoot, tenantOptions.Id)),
                EnvironmentName = envName,
                Id = tenantOptions.Id,
                Urls = tenantOptions.Urls,
                WebRoot = Path.GetFullPath(string.Format(tenantOptions.WebRoot, tenantOptions.Id)),
            };

            if (isDevelopment)
            {
                Directory.CreateDirectory(tenant.ContentRoot);
                Directory.CreateDirectory(tenant.WebRoot);
            }

            try
            {
                var task = (Task)mainMethod.Invoke(null, parameters: [args, tenant, cancellationTokenSource.Token])!;
                tenantsTasks[i] = task;
            }
            catch (Exception ex)
            {
                // Log startup errors and continue loading other tenants
                Log.Critical(typeof(MultiTenantHost), ex, "Tenant failed to start: " + tenant.Id);
                tenantsTasks[i] = Task.CompletedTask;
            }
        }

        // Keep hosting if at least one tenant is still running
        while (!cancellationTokenSource.IsCancellationRequested &&
               tenantsTasks.Any(t => !t.IsCompleted))
        {
            for (int i = 0; i < tenantsTasks.Length; i++)
            {
                var task = tenantsTasks[i];
                if (task.Exception is Exception ex)
                {
                    tenantsTasks[i] = Task.CompletedTask;

                    // Log runtime exceptions and terminate the tenant
                    Log.Critical(typeof(MultiTenantHost), ex, "Tenant crashed: " + tenantsOptions[i].Id);
                    task.Dispose();
                }
            }

            if (tenantsTasks.Any(t => !t.IsCompleted))
            {
                await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }
    }
}