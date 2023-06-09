﻿using DatabaseMod.Models;

namespace DatabaseMod.Alterations.Models;

public class CreateTable : DatabaseAlteration
{
    public CreateTable(string schemaName, string tableName, string? owner, Column[] columns, string[] primaryKey)
        : base(nameof(CreateTable))
    {
        SchemaName = schemaName;
        TableName = tableName;
        Owner = owner;
        Columns = columns;
        PrimaryKey = primaryKey;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public string? Owner { get; }
    public Column[] Columns { get; }
    public string[] PrimaryKey { get; }
}