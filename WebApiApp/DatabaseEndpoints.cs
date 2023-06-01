using DatabaseMod.Models;
using Microsoft.Data.Sqlite;
using SqliteMod;
using System.Data.Common;

namespace WebApiApp;

public class DatabaseEndpoints {

    public static async Task<Database> Database(DbConnection connection) {
        await connection.OpenAsync();

        var database = new Database();
        database.ContributeSqlite((SqliteConnection)connection);
        return database;
    }
}
