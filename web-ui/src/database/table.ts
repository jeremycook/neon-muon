import { FileNode, getDirectoryName } from '../files/files';
import { siteCard } from '../site/siteCard';
import { modalConfirm } from '../ui/modals';
import { dynamic, lazy } from '../utils/dynamicHtml';
import { div, h1, h2, input, table, tbody, td, th, thead, tr } from '../utils/html';
import { jsonPost } from '../utils/http';
import { computed, val } from '../utils/pubSub';
import { getDatabase, Table, Database, getRecords, Column, StoreType, TableIndexType } from './database';

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
                            const [confirmed, newValue] = await modalCell(dbTable.columns[i], item);

                            if (confirmed) {
                                const updateRecord = structuredClone(record);
                                updateRecord[i] = newValue;
                                const response = await jsonPost('/api/update-records', {
                                    path: databasePath,
                                    tableName: dbTable.name,
                                    columnNames: dbTable.columns.map(c => c.name),
                                    records: [updateRecord]
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
                            dynamic(value, () => value.val?.toString() ?? '')
                        )
                    }))
                )
            })
        )
    )
}

async function modalCell<TValue extends (string | number | boolean | Date | null)>(column: Column, value: TValue): Promise<[boolean, TValue | undefined]> {

    let newValue;

    const confirmed = await modalConfirm(
        h2(column.name),
        input({
            value: value?.toString() ?? '',
            autoselect: true,
            onchange(ev: Event) {
                const self = ev.target as HTMLInputElement;
                newValue = changeType(self.value, column.storeType);
            }
        })
    );

    return [confirmed, confirmed ? newValue : undefined];
}

function changeType(value: string, storeType: StoreType) {
    switch (storeType) {
        case StoreType.Text:
        case StoreType.Uuid:
            return value;

        case StoreType.Boolean:
            return new Boolean(value);

        case StoreType.Double:
            return Number.parseFloat(value);

        case StoreType.Integer:
            return Number.parseInt(value);

        case StoreType.Timestamp:
            return Date.parse(value);

        default:
            throw new Error('Not supported store type: ' + storeType);
    }
}

