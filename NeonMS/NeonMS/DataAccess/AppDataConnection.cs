using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using System.Collections.Immutable;

namespace NeonMS.DataAccess;

// TODO? public class AppDataConnection : DataConnection
// {
//     public AppDataConnection(string name)
//         : base(name) { }
// }

// public class AppConnectionStringSettings : IConnectionStringSettings
// {
//     public string ConnectionString { get; set; } = null!;
//     public string Name { get; set; } = null!;
//     public string ProviderName { get; set; } = null!;
//     public bool IsGlobal => false;
// }

// public class AppLinqToDBSettings : ILinqToDBSettings
// {
//     public IEnumerable<IDataProviderSettings> DataProviders { get; } = Enumerable.Empty<IDataProviderSettings>();
//     public string DefaultConfiguration { get; } = ProviderName.PostgreSQL15;
//     public string DefaultDataProvider { get; } = ProviderName.PostgreSQL15;
//     public IEnumerable<IConnectionStringSettings> ConnectionStrings { get; }

//     public AppLinqToDBSettings()
//     {
//         ConnectionStrings = DB.Servers
//             .Select(x => new AppConnectionStringSettings
//             {
//                 Name = x.Key,
//                 ProviderName = ProviderName.PostgreSQL15,
//                 ConnectionString = DB.GetConnectionString( x.Value.ConnectionString,
//             })
//             .ToImmutableArray();
//     }
// }
