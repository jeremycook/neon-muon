import { jsonPost } from '../utils/http';
import { makeUrl } from '../utils/url';
import { Column, TableForeignKey, TableIndex } from './database';

export async function alterDatabase(databasePath: string, ...alterations: DatabaseAlteration[]) {
    const url = makeUrl('/api/alter-database', { path: databasePath });
    const response = await jsonPost(url, alterations);
    return response;
}

export class DatabaseAlteration {
    $type: string;

    constructor(data: DatabaseAlteration) {
        this.$type = data.$type;
    }
}

export class CreateTable extends DatabaseAlteration {
    schemaName: string;
    tableName: string;
    columns: Column[];
    indexes: TableIndex[];
    foreignKeys: TableForeignKey[];
    owner: string | null;

    constructor(data?: {
        schemaName: string;
        tableName: string;
        columns: Column[];
        indexes: TableIndex[];
        foreignKeys: TableForeignKey[];
        owner: string | null;
    }) {
        super({ $type: 'CreateTable' })

        this.schemaName = data?.schemaName ?? '';
        this.tableName = data?.tableName ?? '';
        this.columns = data?.columns.map(column => new Column(column)) ?? [];
        this.indexes = data?.indexes.map(index => new TableIndex(index)) ?? [];
        this.foreignKeys = data?.foreignKeys.map(foreignKey => new TableForeignKey(foreignKey)) ?? [];
        this.owner = data?.owner ?? null;
    }
}

export class DropTable extends DatabaseAlteration {
    schemaName: string;
    tableName: string;

    constructor(data?: {
        schemaName: string;
        tableName: string;
    }) {
        super({ $type: 'DropTable' })

        this.schemaName = data?.schemaName ?? '';
        this.tableName = data?.tableName ?? '';
    }
}

export class CreateColumn extends DatabaseAlteration {
    schemaName: string;
    tableName: string;
    column: Column;

    constructor(data?: {
        schemaName: string;
        tableName: string;
        column: Column;
    }) {
        super({ $type: 'CreateColumn' })

        this.schemaName = data?.schemaName ?? '';
        this.tableName = data?.tableName ?? '';
        this.column = data?.column ?? new Column();
    }
}

export class DropColumn extends DatabaseAlteration {
    schemaName: string;
    tableName: string;
    columnName: string;

    constructor(data?: {
        schemaName: string;
        tableName: string;
        columnName: string;
    }) {
        super({ $type: 'DropColumn' })

        this.schemaName = data?.schemaName ?? '';
        this.tableName = data?.tableName ?? '';
        this.columnName = data?.columnName ?? '';
    }
}