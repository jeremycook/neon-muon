import { modalConfirm } from '../ui/modals';
import icon from '../ui/icons';
import { dynamic } from '../utils/dynamicHtml';
import { button, details, div, h1, h2, input, label, option, p, section, select, summary, table, tbody, td, th, thead, tr } from '../utils/html';
import { jsonGet, jsonPost } from '../utils/http';
import { Pub, val } from '../utils/pubSub';

export async function databaseUI() {

    const database = val(new Database());

    const view = div(
        h1('Database'),
        dynamic(database, () => database.val.schemas.map(schema => div(
            (database.val.schemas.length > 1 && h2(schema.name)),
            p(
                button({ onclick: async () => await createTable(schema, database) },
                    icon("sparkle-16-regular"),
                    ' New Table'
                ),
            ),
            div(
                schema.tables.map(tbl => details(
                    summary(tbl.name),

                    p({ class: 'flex gap-100' },
                        button({ onclick: async () => await createColumn(tbl, schema, database) },
                            icon('sparkle-16-regular'),
                            ' New Column'
                        ),
                        button({ onclick: async () => await dropTable(tbl, schema, database) },
                            icon('delete-12-regular'),
                            ' Delete Table'
                        ),
                    ),

                    table({ class: 'w-100' },
                        thead(
                            tr(
                                th('Name'),
                                th('Store Type'),
                                th('Info'),
                            )
                        ),
                        tbody(...tbl.columns.map(column =>
                            tr(
                                td(column.name),
                                td(column.storeType),
                                td(
                                    ...tbl.indexes.filter(idx => idx.columns.includes(column.name)).map(idx => idx.indexType),
                                    (column.isNullable && ' Optional')
                                ),
                                td(
                                    button({ onclick: async () => await dropColumn(column, tbl, schema, database) },
                                        icon('delete-12-regular')
                                    )
                                )
                            )),
                        )
                    ),
                )),
            )
        )))
    );

    const result = await getDatabase();
    if (result) {
        database.pub(result);
    }

    return view;
}

async function createTable(schema: Schema, database: Pub) {
    const tableName = prompt('New Table Name');
    if (tableName) {
        const codeName = tableName.replace(' ', '');
        const primaryKeyColumnName = codeName + 'Id';
        const newTable = {
            '$type': 'CreateTable',
            schemaName: schema.name,
            tableName: tableName,
            owner: null,
            columns: [new Column({
                name: primaryKeyColumnName,
                storeType: StoreType.Integer,
                isNullable: false,
            })],
            primaryKey: [primaryKeyColumnName],
        };
        const response = await alterDatabase(newTable);

        if (response.ok) {
            schema.tables.push(new Table({ name: newTable.tableName, columns: newTable.columns, indexes: [new TableIndex({ name: 'pk_' + codeName, indexType: TableIndexType.primaryKey, columns: [primaryKeyColumnName] })] }));
            database.pub();
        }
        else {
            alert(response.errorMessage ?? 'An error occurred.');
        }
    }
}

async function dropTable(tbl: Table, schema: Schema, success: Pub) {

    if (!confirm(`Are you sure you want to delete the ${[schema.name, tbl.name].filter(x => x?.length).join('.')} table? This cannot be undone.`)) {
        return;
    }

    const response = await alterDatabase({
        '$type': 'DropTable',
        schemaName: schema.name,
        tableName: tbl.name,
    });

    if (response.ok) {
        schema.tables.splice(schema.tables.indexOf(tbl), 1);
        success.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

async function dropColumn(column: Column, tbl: Table, schema: Schema, database: Pub) {

    if (!confirm(`Are you sure you want to delete the ${[schema.name, tbl.name, column.name].filter(x => x?.length).join('.')} column? This cannot be undone.`)) {
        return;
    }

    const response = await alterDatabase({
        '$type': 'DropColumn',
        schemaName: schema.name,
        tableName: tbl.name,
        columnName: column.name,
    });

    if (response.ok) {
        tbl.columns.splice(tbl.columns.indexOf(column), 1);
        database.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

async function createColumn(tbl: Table, schema: Schema, success: Pub) {

    const data = {
        name: '',
        storeType: StoreType.Text,
        isNullable: true,
    };

    const confirmed = await modalConfirm(
        section(
            h1('Create Column'),
            label(
                div('Column Name'),
                input({
                    value: data.name,
                    required: true,
                    oninput(ev: { target: HTMLInputElement }) { data.name = ev.target.value },
                    onmount(ev: { target: HTMLInputElement }) { ev.target.focus() },
                }),
            ),
            label(
                div('Type'),
                select(
                    {
                        value: data.storeType,
                        required: true,
                        onchange(ev: { target: HTMLSelectElement }) { data.storeType = ev.target.value as StoreType }
                    },
                    ...[StoreType.Blob, StoreType.Boolean, StoreType.Double, StoreType.Guid, StoreType.Integer, StoreType.Text, StoreType.Timestamp]
                        .map(storeType => option({ value: storeType, selected: data.storeType === storeType }, storeType))
                )
            ),
            label(
                div('Optional'),
                input({
                    type: 'checkbox',
                    checked: data.isNullable,
                    onchange(ev: { target: HTMLInputElement }) { data.isNullable = ev.target.checked }
                })
            ),
        )
    );

    if (!confirmed) {
        return;
    }

    const column = new Column(data);
    const response = await alterDatabase({
        '$type': 'CreateColumn',
        schemaName: schema.name,
        tableName: tbl.name,
        column,
    });

    if (response.ok) {
        tbl.columns.push(column);
        success.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

//#region API

export async function getDatabase() {
    const response = await jsonGet<Database>('/api/database');
    if (response.result) {
        return new Database(response.result);
    }
}

export async function alterDatabase(...alterations: any[]) {
    const response = await jsonPost('/api/alter-database', alterations);
    return response;
}

class Database {
    public schemas: Schema[];

    constructor(data?: Database) {
        this.schemas = data?.schemas?.map(o => new Schema(o)) ?? [];
    }
};

class Schema {
    public name: string;
    public tables: Table[];

    constructor(data?: Schema) {
        this.name = data?.name ?? '';
        this.tables = data?.tables?.map(o => new Table(o)) ?? [];
    }
}

class Table {
    public name: string;
    public columns: Column[];
    public indexes: TableIndex[];
    constructor(data?: Table) {
        this.name = data?.name ?? '';
        this.indexes = data?.indexes?.map(o => new TableIndex(o)) ?? [];
        this.columns = data?.columns?.map(o => new Column(o)) ?? [];
    }
}

enum StoreType {
    Blob = 'Blob',
    Boolean = 'Boolean',
    Double = 'Double',
    Guid = 'Guid',
    Integer = 'Integer',
    Text = 'Text',
    Timestamp = 'Timestamp',
}

class Column {
    public name: string;
    public storeType: StoreType;
    public isNullable: boolean;

    constructor(data?: Column) {
        this.name = data?.name ?? '';
        this.storeType = data?.storeType ?? StoreType.Text;
        this.isNullable = data?.isNullable ?? true;
    }
}

enum TableIndexType {
    index = 'Index',
    uniqueConstraint = 'UniqueConstraint',
    primaryKey = 'PrimaryKey',
}

class TableIndex {
    public name: string;
    public indexType: TableIndexType;
    public columns: string[];

    constructor(data?: TableIndex) {
        this.name = data?.name ?? '';
        this.indexType = data?.indexType ?? TableIndexType.index;
        this.columns = data?.columns ?? [];
    }
}

//#endregion API
