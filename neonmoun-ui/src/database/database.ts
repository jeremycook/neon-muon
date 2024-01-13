import { FileNode, getParentPath, promptDeleteFile, promptMoveFile, refreshRoot } from '../files/files';
import { icon } from '../ui/icons';
import { modalConfirm, modalPrompt } from '../ui/modals';
import { dynamic } from '../utils/dynamicHtml';
import { a, button, details, div, h1, h2, input, label, option, p, section, select, summary, table, tbody, td, th, thead, tr } from '../utils/html';
import { jsonGet, jsonPost } from '../utils/http';
import { Pub, val } from '../utils/pubSub';
import { makeUrl, redirect } from '../utils/url';
import { CreateColumn, CreateTable, DropColumn, DropTable, alterDatabase } from './alterDatabase';

export async function databaseApp({ fileNode }: { fileNode: FileNode }) {

    const path = fileNode.path;
    const database = val(new Database());

    const view = div(
        h1(fileNode.name),

        div({ class: 'flex mb' },
            a({ class: 'button', href: makeUrl('/api/download-file', { path: fileNode.path }) },
                icon('arrow-download-regular'), ' Download'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const newFilePath = await promptMoveFile(fileNode);
                    if (newFilePath) {
                        redirect(makeUrl('/browse', { path: newFilePath }));
                        return;
                    }
                }
            },
                icon('rename-regular'), ' Move'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const success = await promptDeleteFile(fileNode);
                    if (success) {
                        redirect(makeUrl('/browse', { path: getParentPath(fileNode.path) }));
                        return;
                    }
                }
            },
                icon('delete-regular'), ' Delete'
            ),
        ),

        dynamic(database, () => database.value.schemas.map(schema =>
            div({ class: 'card' }, {
                ondragover(ev: DragEvent) { ev.preventDefault(); },
                async ondrop(ev: DragEvent) {
                    ev.preventDefault();
                    ev.stopPropagation();

                    const json = ev.dataTransfer!.getData('text/x-file-node');
                    if (json) {
                        const sourceNode = <FileNode>JSON.parse(ev.dataTransfer!.getData('text/x-file-node'));
                        const response = await createTableBasedOnFileNode(fileNode.path, sourceNode.path);
                        if (response.errorMessage) {
                            alert(response.errorMessage || 'An error occurred while trying to move a file.');
                        }
                        await refreshRoot();
                    }

                    // TODO? const files = getFilesFromDataTransfer(ev.dataTransfer);
                    // if (files.length > 0) {
                    //     const response = await uploadFiles(destinationDirectory, files);
                    //     if (!response.ok) {
                    //         alert(await response.text() || 'An error occurred while trying to upload files.');
                    //     }
                    //     await refreshRoot();
                    // }
                }
            },
                (database.value.schemas.length > 1 && h2(schema.name)),
                p(
                    button({ onclick: async () => await createTable(schema, database, path) },
                        icon("sparkle-regular"),
                        ' New Table'
                    ),
                ),
                div(...schema.tables.map(tbl =>
                    details({ open: true },
                        summary(tbl.name),

                        p({ class: 'flex gap-100' },
                            button({ onclick: async () => await createColumn(tbl, schema, database, path) },
                                icon('sparkle-regular'),
                                ' New Column'
                            ),
                            button({ onclick: async () => await dropTable(tbl, schema, database, path) },
                                icon('delete-regular'),
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
                                        button({ onclick: async () => await dropColumn(column, tbl, schema, database, path) },
                                            icon('delete-regular')
                                        ),
                                    )
                                )),
                            )
                        ),
                    )),
                )
            )
        ))
    );

    const result = await getDatabase(path);
    if (result) {
        database.value = result;
    }

    return view;
}

export async function moveFile(path: string, newPath: string) {
    return await jsonPost('/api/move-file', {
        path,
        newPath,
    });
}

async function createTable(schema: Schema, onSuccess: Pub, databasePath: string) {
    const tableName = await modalPrompt('New Table');
    if (tableName) {
        const codeName = tableName.replace(/[^A-Za-z0-9_]/g, '');
        const primaryKeyColumnName = codeName + 'Id';
        const newTable = new CreateTable({
            schemaName: schema.name,
            tableName: tableName,
            owner: null,
            columns: [new Column({
                name: primaryKeyColumnName,
                storeType: StoreType.Integer,
                isNullable: false,
            })],
            indexes: [new TableIndex({ name: null, indexType: TableIndexType.primaryKey, columns: [primaryKeyColumnName] })],
            foreignKeys: [],
        });
        const response = await alterDatabase(databasePath, newTable);

        if (response.ok) {
            schema.tables.push(new Table({
                name: newTable.tableName,
                columns: newTable.columns,
                indexes: newTable.indexes,
                foreignKeys: [],
                owner: null,
            }));
            onSuccess.pub();
        }
        else {
            alert(response.errorMessage ?? 'An error occurred.');
        }
    }
}

async function dropTable(tbl: Table, schema: Schema, onSuccess: Pub, databasePath: string) {

    if (!await modalConfirm(`Are you sure you want to delete the ${[schema.name, tbl.name].filter(x => x?.length).join('.')} table? This cannot be undone.`)) {
        return;
    }

    const response = await alterDatabase(databasePath, new DropTable({
        schemaName: schema.name,
        tableName: tbl.name,
    }));

    if (response.ok) {
        schema.tables.splice(schema.tables.indexOf(tbl), 1);
        onSuccess.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

async function dropColumn(column: Column, tbl: Table, schema: Schema, onSuccess: Pub, databasePath: string) {

    if (!confirm(`Are you sure you want to delete the ${[schema.name, tbl.name, column.name].filter(x => x?.length).join('.')} column? This cannot be undone.`)) {
        return;
    }

    const response = await alterDatabase(databasePath, new DropColumn({
        schemaName: schema.name,
        tableName: tbl.name,
        columnName: column.name,
    }));

    if (response.ok) {
        tbl.columns.splice(tbl.columns.indexOf(column), 1);
        onSuccess.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

async function createColumn(tbl: Table, schema: Schema, onSuccess: Pub, databasePath: string) {

    const column = new Column();

    const confirmed = await modalConfirm(
        section(
            h2('New Column'),
            label(
                div('Column Name'),
                input({
                    value: column.name,
                    required: true,
                    autofocus: true,
                    oninput(ev: { target: HTMLInputElement }) { column.name = ev.target.value },
                }),
            ),
            label(
                div('Type'),
                select(
                    {
                        value: column.storeType,
                        required: true,
                        onchange(ev: { target: HTMLSelectElement }) { column.storeType = ev.target.value as StoreType }
                    },
                    ...Object.values(StoreType)
                        .map(storeType => option({ value: storeType, selected: column.storeType === storeType }, storeType))
                )
            ),
            label(
                div('Optional'),
                input({
                    type: 'checkbox',
                    checked: column.isNullable,
                    onchange(ev: { target: HTMLInputElement }) { column.isNullable = ev.target.checked }
                })
            ),
        )
    );

    if (!confirmed) {
        return;
    }

    const response = await alterDatabase(databasePath, new CreateColumn({
        schemaName: schema.name,
        tableName: tbl.name,
        column,
    }));

    if (response.ok) {
        tbl.columns.push(column);
        onSuccess.pub();
    }
    else {
        alert(response.errorMessage ?? 'An error occurred.');
    }
}

//#region API

export async function createTableBasedOnFileNode(databasePath: string, sourcePath: string) {
    const url = makeUrl('/api/create-table-based-on-file-node', { path: databasePath });
    const data = { sourcePath };
    const response = await jsonPost(url, data);
    return response;
}

export async function getDatabase(databasePath: string) {
    const url = makeUrl('/api/get-database', { path: databasePath });
    const response = await jsonGet<Database>(url);
    if (response.result) {
        return new Database(response.result);
    }
    else {
        return undefined;
    }
}

export class Database {
    public schemas: Schema[];

    constructor(data?: Database) {
        this.schemas = data?.schemas?.map(o => new Schema(o)) ?? [];
    }
};

export class Schema {
    public name: string;
    public tables: Table[];

    constructor(data?: Schema) {
        this.name = data?.name ?? '';
        this.tables = data?.tables?.map(o => new Table(o)) ?? [];
    }
}

export class Table {
    name: string;
    columns: Column[];
    indexes: TableIndex[];
    foreignKeys: TableForeignKey[];
    owner: string | null;
    constructor(data?: Table) {
        this.name = data?.name ?? '';
        this.columns = data?.columns?.map(o => new Column(o)) ?? [];
        this.indexes = data?.indexes?.map(o => new TableIndex(o)) ?? [];
        this.foreignKeys = data?.foreignKeys?.map(o => new TableForeignKey(o)) ?? [];
        this.owner = data?.owner ?? null;
    }
}

export enum StoreType {
    General = 'General',
    Text = 'Text',
    Blob = 'Blob',
    Boolean = 'Boolean',
    Date = 'Date',
    Integer = 'Integer',
    Numeric = 'Numeric',
    Real = 'Real',
    Time = 'Time',
    Timestamp = 'Timestamp',
    Uuid = 'Uuid',
}

export class Column {
    public name: string;
    public storeType: StoreType;
    public isNullable: boolean;

    constructor(data?: Column) {
        this.name = data?.name ?? '';
        this.storeType = data?.storeType ?? StoreType.General;
        this.isNullable = data?.isNullable ?? true;
    }
}

export enum TableIndexType {
    index = 'Index',
    uniqueConstraint = 'UniqueConstraint',
    primaryKey = 'PrimaryKey',
}

export class TableIndex {
    public name: string | null;
    public indexType: TableIndexType;
    public columns: string[];

    constructor(data?: TableIndex) {
        this.name = data?.name ?? null;
        this.indexType = data?.indexType ?? TableIndexType.index;
        this.columns = data?.columns ?? [];
    }
}

export class TableForeignKey {
    name: string | null;
    columns: string[];
    foreignSchemaName: string;
    foreignTableName: string;
    foreignColumns: string[];

    constructor(data?: TableForeignKey) {
        this.name = data?.name ?? '';
        this.columns = data?.columns.slice() ?? [];
        this.foreignSchemaName = data?.foreignSchemaName ?? '';
        this.foreignTableName = data?.foreignTableName ?? '';
        this.foreignColumns = data?.foreignColumns.slice() ?? [];
    }
}

//#endregion API
