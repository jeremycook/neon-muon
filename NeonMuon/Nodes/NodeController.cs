using Microsoft.AspNetCore.Mvc;
using NeonMuon.Authentication;
using NeonMuon.DataAccess;
using NeonMuon.Mvc;
using Npgsql;

namespace NeonMuon.Nodes;

[ApiController]
[Route(MvcConstants.StandardApiRoute)]
public class NodeController(
    DB DB,
    DataServers DataServers,
    CurrentUser CurrentUser
) : Controller
{
    public class GraphInput
    {
        public string Path { get; set; } = string.Empty;
    }

    [HttpGet("/api/get-file-node")]
    public async Task<ActionResult<FileNode>>
    GetFileNode(
        string? path,
        CancellationToken cancellationToken
    )
    {
        path ??= string.Empty;

        return new FileNode()
        {
            Name = "Root",
            Path = "",
            IsExpandable = true,
            Children = await GetDataServerNodes(CurrentUser.Credentials(), cancellationToken),
        };
    }

    [HttpPut]
    public async Task<ActionResult<FileNode>>
    Graph(
        GraphInput input,
        CancellationToken cancellationToken
    )
    {
        return new FileNode()
        {
            Name = "Root",
            Path = "",
            IsExpandable = true,
            Children = await GetDataServerNodes(CurrentUser.Credentials(), cancellationToken),
        };
    }

    private async Task<List<FileNode>> GetDataServerNodes(IEnumerable<DataCredential> credentials, CancellationToken cancellationToken)
    {
        var list = new List<FileNode>();
        foreach (var credential in credentials)
        {
            var dataServer = DataServers[credential.Server];
            using var con = await DB.OpenConnection(credential, dataServer.MaintenanceDatabase, cancellationToken);
            using var cmd = new NpgsqlCommand("""
                select datname catalog_name
                from pg_catalog.pg_database
                where datdba > 1000
                order by datname
            """, con);
            var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var databaseNames = await reader.ListAsync<string>(cancellationToken);

            var serverNode = new FileNode()
            {
                Name = credential.Server,
                Path = credential.Server,
                IsExpandable = true,
                Children = databaseNames
                    .Select(db => new FileNode()
                    {
                        Name = db,
                        Path = credential.Server + "/" + db,
                        IsExpandable = true,
                        Children = [],
                    })
                    .ToList()
            };
            list.Add(serverNode);
        }
        return list;
    }

    // private async Task<List<FileNode>> GetDataServerNodes(DataCredential credential)
    // {
    //     var dataServer = DataServers[credential.Server];
    //     DB.OpenConnection(credential,)
    // }
}

public class FileNode
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required bool IsExpandable { get; set; }
    public required List<FileNode> Children { get; set; }
}