import { FileNode, getDirectoryName } from '../files/files';
import { siteCard } from '../site/siteCard';
import { modalConfirm } from '../ui/modals';
import { dynamic, lazy } from '../utils/dynamicHtml';
import { div, h1, h2, input, table, tbody, td, textarea, th, thead, tr } from '../utils/html';
import { jsonPost } from '../utils/http';
import { computed, val } from '../utils/pubSub';
import { unreachable } from '../utils/unreachable';
import { getDatabase, Table, Database, getRecords, Column, StoreType, TableIndexType, Primitive } from './database';

export async function tableApp({ fileNode: tableNode }: { fileNode: FileNode }) {

    const databasePath = getDirectoryName(tableNode.path);
    const database = (await getDatabase(databasePath))!;
    const table = database.schemas[0].tables.find(t => t.name === tableNode.name)!;

    return siteCard(
        h1(tableNode.name),
        div(
            databaseTable(table, database, databasePath)
        )
    )
}

function databaseTable(dbTable: Table, _database: Database, databasePath: string) {

    const pkColumns = dbTable.indexes
        .find(idx => idx.indexType === TableIndexType.primaryKey)
        ?.columns
        ?.map(name => dbTable.columns.findIndex(col => col.name === name))
        ?? [];

    return table({ class: 'bordered' },
        thead(
            tr(...dbTable.columns.map(col =>
                th({ tabIndex: 0 }, col.name)
            ))
        ),
        tbody(
            lazy(async () => {
                const records = await getRecords(databasePath, dbTable.name, dbTable.columns.map(col => col.name));

                return records.map(record =>
                    tr(...record.map((item, i) => {
                        const value = val(item);

                        computed(value, async () => {
                            // Update state
                            record[i] = value.val;
                        });

                        const editValue = async (ev: Event): Promise<void> => {
                            const newValue = await modalCell(dbTable.columns[i], item);

                            if (typeof newValue !== 'undefined') {
                                const response = await jsonPost('/api/update-records', {
                                    path: databasePath,
                                    tableName: dbTable.name,
                                    columnNames: buildTableModificationColumns(dbTable.columns, pkColumns, i),
                                    records: [buildTableModificationRecord(record, pkColumns, newValue)]
                                });
                                if (response.ok) {
                                    value.pub(newValue);
                                }
                                else {
                                    alert(response.errorMessage ?? 'Something went wrong.');
                                }
                            }

                            setTimeout(() => (ev.target as any)?.focus?.(), 200);
                        };

                        return td({ tabIndex: 0 },
                            !pkColumns.includes(i) && {
                                async ondblclick(ev: Event) {
                                    await editValue(ev);
                                },
                                async onkeyup(ev: KeyboardEvent) {
                                    if (ev.key === 'F2' || ev.key === 'Enter' || ('a' <= ev.key && ev.key <= 'z')) {
                                        await editValue(ev);
                                    }
                                }
                            },
                            dynamic(value, () => storeTypeToString(value.val, dbTable.columns[i].storeType))
                        )
                    }))
                )
            })
        )
    )
}

function buildTableModificationColumns(columns: Column[], pkColumns: number[], ...valueColumn: number[]) {
    return pkColumns.concat(valueColumn).map(i => columns[i].name);
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
                        newValue = stringToStoreType(self.value, column.storeType);
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
                        newValue = stringToStoreType(self.value, column.storeType);
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

        case StoreType.Currency:
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
            unreachable(storeType);
    }

    return value?.toString() ?? '';
}

function stringToStoreType(value: string, storeType: StoreType) {
    switch (storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + storeType);

        case StoreType.Text:
        case StoreType.Uuid:
            return value;

        case StoreType.Boolean:
            return new Boolean(value);

        case StoreType.Currency:
        case StoreType.Real:
            return Number.parseFloat(value);

        case StoreType.Integer:
            return Number.parseInt(value);

        case StoreType.Date:
        case StoreType.Time:
        case StoreType.Timestamp:
            return new Date(value);

        default:
            unreachable(storeType);
    }
}

