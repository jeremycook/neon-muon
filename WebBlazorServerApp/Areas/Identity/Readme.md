# Development

```sh
dotnet tool update dotnet-ef

cd WebBlazorServerApp
dotnet ef migrations add --context IdentityDbContext --output-dir Areas/Identity/Data/Migrations MigrationName
dotnet ef migrations remove --context IdentityDbContext [--force]
dotnet ef database update --context IdentityDbContext
```
