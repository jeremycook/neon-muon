import { FileNode, getDirectoryName } from '../files/files';
import { siteCard } from '../site/siteCard';
import icon from '../ui/icons';
import { modalConfirm } from '../ui/modals';
import { dynamic, lazy } from '../utils/dynamicHtml';
import { EventT } from '../utils/etc';
import { button, div, h1, h2, input, label, p, table, tbody, td, textarea, th, thead, tr } from '../utils/html';
import { jsonPost } from '../utils/http';
import { PubT, val } from '../utils/pubSub';
import { UnreachableError } from '../utils/unreachable';
import { getDatabase, Table, Database, Column, StoreType, TableIndexType, Primitive, Schema } from './database';
import { selectRecords } from './records';

export async function tableApp({ fileNode: tableNode }: { fileNode: FileNode }) {

    const databasePath = getDirectoryName(tableNode.path);
    const database = (await getDatabase(databasePath))!;
    const schema = database.schemas[0];
    const tableInfo = schema.tables.find(t => t.name === tableNode.name)!;

    return siteCard(
        h1(tableNode.name),
        ...lazy(
            databaseTable(tableInfo, schema, database, databasePath),
            p('Loading...')
        )
    )
}

async function databaseTable(tableInfo: Table, schema: Schema, _database: Database, databasePath: string) {

    const pkColumns = tableInfo.indexes
        .find(idx => idx.indexType === TableIndexType.primaryKey)
        ?.columns
        ?.map(name => tableInfo.columns.findIndex(col => col.name === name))
        ?? [];

    const records = val(await selectRecords(databasePath, schema.name, tableInfo.name, tableInfo.columns.map(col => col.name)));

    return div(
        p(
            button({ class: 'button' }, {
                async onclick() {
                    const newRecordColumns = getNewRecordColumns(tableInfo);
                    const newRecord = createNewRecord(newRecordColumns)
                        .map(val);

                    const confirmed = await modalConfirm(
                        h2('New Record'),
                        ...newRecordColumns
                            .map((column, i) =>
                                label(
                                    div(column.name),
                                    valueEditor(column, newRecord[i])
                                )
                            )
                    );

                    if (!confirmed) {
                        return;
                    }

                    const response = await postInsertRecords(databasePath, schema.name, tableInfo.name, newRecordColumns.map(x => x.name), tableInfo.columns.map(column => column.name), [newRecord.map(x => x.val)]);
                    if (response.result) {
                        // Insert the returned records
                        records.pub([
                            ...response.result,
                            ...records.val,
                        ]);
                    }
                    else {
                        alert(response.errorMessage ?? 'Something went wrong.');
                    }
                }
            },
                icon('sparkle-regular'), ' New Record')
        ),

        table({ class: 'bordered' },
            thead(
                tr(...tableInfo.columns.map(col =>
                    th({ tabIndex: 0 }, col.name)
                ))
            ),
            tbody(dynamic(records, () => records.val.map(record =>
                tr(...record.map((item, i) => {
                    const value = val(item);

                    value.sub(record, async () => {
                        // Update state
                        record[i] = value.val;
                    });

                    async function editValue(ev: Event): Promise<void> {
                        const newValue = await modalCell(tableInfo.columns[i], item);

                        if (typeof newValue !== 'undefined') {
                            const response = await postUpdateRecords(databasePath, schema.name, tableInfo, pkColumns, i, record, newValue);
                            if (response.ok) {
                                value.pub(newValue);
                            }
                            else {
                                alert(response.errorMessage ?? 'Something went wrong.');
                            }
                        }

                        // Return focus to source
                        (ev.target as any)?.focus?.();
                    }

                    return td({ tabIndex: 0 },
                        !pkColumns.includes(i) && {
                            async ondblclick(ev: Event) {
                                await editValue(ev);
                            },
                            async onkeyup(ev: KeyboardEvent) {
                                if (ev.key === 'F2' || ev.key === 'Enter' || ev.key === ' ') {
                                    await editValue(ev);
                                }
                            }
                        },
                        dynamic(value, () => storeTypeToString(value.val, tableInfo.columns[i].storeType))
                    )
                }))
            )
            ))
        )
    )
}

async function postInsertRecords(databasePath: string, schema: string, table: string, columns: string[], returningColumns: string[], newRecords: (Primitive | null)[][]) {
    return await jsonPost<(Primitive | null)[][]>('/api/insert-records', {
        database: databasePath,
        schema,
        table,
        columns,
        records: newRecords,
        returningColumns,
    });
}

async function postUpdateRecords(databasePath: string, schema: string, tableInfo: Table, pkColumns: number[], i: number, record: (Primitive | null)[], newValue: Primitive | null) {
    return await jsonPost('/api/update-records', {
        database: databasePath,
        schema,
        table: tableInfo.name,
        columns: buildTableModificationColumns(tableInfo.columns, pkColumns, i),
        records: [buildTableModificationRecord(record, pkColumns, newValue)]
    });
}

async function postDeleteRecords(databasePath: string, schema: string, tableInfo: Table, pkColumns: number[], i: number, record: (Primitive | null)[], newValue: Primitive | null) {
    return await jsonPost('/api/delete-records', {
        database: databasePath,
        schema,
        table: tableInfo.name,
        columns: buildTableModificationColumns(tableInfo.columns, pkColumns, i),
        records: [buildTableModificationRecord(record, pkColumns, newValue)]
    });
}

function buildTableModificationColumns(columns: Column[], pkColumns: number[], ...valueColumns: number[]) {
    return pkColumns.concat(valueColumns).map(i => columns[i].name);
}

function buildTableModificationRecord(record: (Primitive | null)[], pkColumns: number[], ...value: (Primitive | null)[]) {
    return [...pkColumns.map(i => record[i]), ...value];
}

async function modalCell<TValue extends Primitive | null>(column: Column, value: TValue): Promise<TValue | undefined> {

    let newValue;

    const confirmed = await modalConfirm(
        h2(column.name),
        column.storeType === StoreType.Text
            ? textarea(
                {
                    autoselect: true,
                    onchange(ev: Event) {
                        const self = ev.target as HTMLInputElement;
                        newValue = stringToStoreType(self.value, column.storeType, column.isNullable);
                    },
                    onkeydown(ev: KeyboardEvent) {
                        if (ev.ctrlKey && ev.key === 'Enter') {
                            ev.preventDefault();
                        }
                    }
                },
                value?.toString() ?? ''
            )
            : input(
                {
                    value: storeTypeToString(value, column.storeType),
                    autoselect: true,
                    onchange(ev: Event) {
                        const self = ev.target as HTMLInputElement;
                        newValue = stringToStoreType(self.value, column.storeType, column.isNullable);
                    },
                    onkeydown(ev: KeyboardEvent) {
                        if (ev.ctrlKey && ev.key === 'Enter') {
                            ev.preventDefault();
                        }
                    }
                },
            )
    );

    return confirmed ? newValue : undefined;
}

function storeTypeToString(value: Primitive | null, storeType: StoreType): string {
    switch (storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + storeType);

        case StoreType.Text:
        case StoreType.Uuid:
            return value?.toString() ?? '';

        case StoreType.Boolean:
            return value?.toString() ?? '';

        case StoreType.Numeric:
        case StoreType.Real:
            return value?.toString() ?? '';

        case StoreType.Integer:
            return value?.toString() ?? '';

        case StoreType.Date:
            return value instanceof Date ? value.toDateString() : value?.toString() ?? '';
        case StoreType.Time:
            return value instanceof Date ? value.toTimeString() : value?.toString() ?? '';
        case StoreType.Timestamp:
            return value instanceof Date ? value.toISOString() : value?.toString() ?? '';

        default:
            throw new UnreachableError(storeType);
    }
}

function stringToStoreType(value: string, storeType: StoreType, isNullable: boolean): Primitive | null {
    if (isNullable && !value) {
        // Treat empty strings as null
        return null;
    }

    switch (storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + storeType);

        case StoreType.Text:
        case StoreType.Uuid:
            return value;

        case StoreType.Boolean:
            return value.startsWith('t') || value.startsWith('T');

        case StoreType.Numeric:
        case StoreType.Real:
            return value ? Number.parseFloat(value.replace(',', '')) : null; // Avoid NaN

        case StoreType.Integer:
            return value ? Number.parseInt(value.replace(',', '')) : null; // Avoid NaN

        case StoreType.Date:
        case StoreType.Time:
        case StoreType.Timestamp:
            return value ? new Date(value) : null;

        default:
            throw new UnreachableError(storeType);
    }
}

function getNewRecordColumns(tableInfo: Table) {
    const pkColumns = getPrimaryKeyIndexes(tableInfo);

    return tableInfo.columns
        .filter((_, i) => !pkColumns.includes(i));
}

function getPrimaryKeyIndexes(tableInfo: Table) {
    return tableInfo.indexes
        .find(idx => idx.indexType === TableIndexType.primaryKey)
        ?.columns
        ?.map(name => tableInfo.columns.findIndex(col => col.name === name))
        ?? [];
}

function createNewRecord(columns: Column[]) {
    return columns
        .map(column => stringToStoreType('', column.storeType, column.isNullable));
}

function valueEditor(column: Column, value: PubT<Primitive | null>) {
    switch (column.storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + column.storeType);

        case StoreType.Text:
        case StoreType.Uuid:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.val, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.pub(stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable)); }
            });

        case StoreType.Boolean:
            return div({ class: 'value-editor-' + column.storeType },
                label(
                    input({ type: 'radio' }, {
                        value: 'true',
                        checked: value.val === true,
                        onchange() { value.pub(true); }
                    }),
                    'Yes'
                ),
                label(
                    input({ type: 'radio' }, {
                        value: 'false',
                        checked: value.val !== true,
                        onchange() { value.pub(false); }
                    }),
                    'No'
                )
            );

        case StoreType.Numeric:
        case StoreType.Real:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.val, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.pub(stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable)); }
            });

        case StoreType.Integer:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.val, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.pub(stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable)); }
            });

        case StoreType.Date:
        case StoreType.Time:
        case StoreType.Timestamp:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.val, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.pub(stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable)); }
            });

        default:
            throw new UnreachableError(column.storeType);
    }
}
