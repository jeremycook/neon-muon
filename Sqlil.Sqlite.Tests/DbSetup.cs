using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Sqlil.Sqlite.Tests;

public static class DbSetup {
    public const string Ddl = """
        CREATE TABLE "User" (
            "UserId" INTEGER NOT NULL,
            "Username" TEXT NOT NULL UNIQUE,
            "IsActive" INTEGER NOT NULL DEFAULT 1,
            "Birthday" TEXT,
            "Created" TEXT NOT NULL DEFAULT (datetime()),
            PRIMARY KEY("UserId" AUTOINCREMENT)
        );

        CREATE TABLE "Role" (
            "RoleId" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            PRIMARY KEY("RoleId")
        );

        CREATE TABLE "UserRole" (
            "UserId" INTEGER NOT NULL,
            "RoleId" TEXT NOT NULL,
            PRIMARY KEY("UserId", "RoleId"),
            FOREIGN KEY("UserId") REFERENCES "User"("UserId"),
            FOREIGN KEY("RoleId") REFERENCES "Role"("RoleId")
        );
        """;

    public static void Seed(DbConnection connection) {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "User" ("Username", "IsActive", "Birthday") VALUES ('Alice', 1, '1990-01-15');
            INSERT INTO "User" ("Username", "IsActive", "Birthday") VALUES ('Bob', 1, '1985-07-20');
            INSERT INTO "User" ("Username", "IsActive", "Birthday") VALUES ('Charlie', 0, NULL);

            INSERT INTO "Role" ("RoleId", "Name") VALUES ('0ed5476c-8cb9-44a9-9db4-8dc45990d997', 'Admin');
            INSERT INTO "Role" ("RoleId", "Name") VALUES ('456ecbd1-c5a9-4384-9a6e-e37dbefdb630', 'New');

            INSERT INTO "UserRole" ("UserId", "RoleId") VALUES (1, '0ed5476c-8cb9-44a9-9db4-8dc45990d997');
            INSERT INTO "UserRole" ("UserId", "RoleId") VALUES (2, '456ecbd1-c5a9-4384-9a6e-e37dbefdb630');
            """;
        cmd.ExecuteNonQuery();
    }

    public static SqliteConnection CreateSeeded() {
        var connection = SqliteConnectionExtensions.OpenInMemory();
        connection.ExecuteNonQuery(Ddl);
        Seed(connection);
        return connection;
    }
}
