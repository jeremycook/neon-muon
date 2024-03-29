import { icon } from '../ui/icons';
import { modalConfirm } from '../ui/modals';
import { dynamic } from '../utils/dynamicHtml';
import { EventT } from '../utils/etc';
import { button, div, h2, input, label, table, tbody, td, textarea, th, thead, tr } from '../utils/html';
import { Val, val } from '../utils/pubSub';
import { Unreachable } from '../utils/exceptions';
import { Column, Schema, StoreType, Table, TableIndexType } from './database';
import { Primitive } from '../utils/types';
import { deleteRecords, insertRecords, selectRecords, updateRecords } from './records';

export async function databaseTable(tableInfo: Table, schema: Schema, databasePath: string) {

    const pkColumns = tableInfo.indexes
        .find(idx => idx.indexType === TableIndexType.primaryKey)
        ?.columns
        ?.map(name => tableInfo.columns.findIndex(col => col.name === name))
        ?? [];

    const response = await selectRecords(databasePath, schema.name, tableInfo.name, tableInfo.columns.map(col => col.name));
    const result = response.getResultOrThrow();
    const rows = val(result.map(record => ({
        selected: val(false),
        record
    })));

    return div(
        div({ class: 'flex gap mb' },
            button({ class: 'button' }, {
                async onclick() {
                    const newRecordColumns = getNewRecordColumns(tableInfo);
                    const newRecord = createNewRecord(newRecordColumns)
                        .map(val);

                    const confirmed = await modalConfirm(
                        h2('New Record'),
                        ...newRecordColumns
                            .map((column, i) => label(
                                div(column.name),
                                valueEditor(column, newRecord[i])
                            )
                            )
                    );

                    if (!confirmed) {
                        return;
                    }

                    const response = await insertRecords(databasePath, schema.name, tableInfo.name, newRecordColumns.map(x => x.name), tableInfo.columns.map(column => column.name), [newRecord.map(x => x.value)]);
                    if (response.result) {
                        // Insert the returned records
                        rows.value = [
                            ...response.result.map(record => ({
                                selected: val(true),
                                record
                            })),
                            ...rows.value,
                        ];
                    }
                    else {
                        alert(response.errorMessage || 'Something went wrong.');
                    }
                }
            },
                icon('sparkle-regular'), ' New Record'
            ),
            button({ class: 'button' }, {
                async onclick() {
                    const selectedRecords = rows.value
                        .filter(row => row.selected.value)
                        .map(row => row.record);

                    if (selectedRecords.length === 0) {
                        await modalConfirm('No records are selected.');
                        return;
                    }

                    const confirmed = await modalConfirm('Delete ' + selectedRecords.length + ' records?');
                    if (!confirmed) {
                        return;
                    }

                    const response = await deleteRecords(databasePath, schema.name, tableInfo.name, tableInfo.columns.map(column => column.name), selectedRecords);
                    if (response.ok) {
                        // Remove the deleted records
                        rows.value = rows.value.filter(row => !selectedRecords.includes(row.record));
                    }
                    else {
                        alert(response.errorMessage || 'Something went wrong.');
                    }
                }
            },
                icon('delete-regular'), ' Delete Selected Records'
            )
        ),

        table({ class: 'bordered' },
            thead(
                tr(
                    th(
                        input({ type: 'checkbox' }, {
                            onchange(ev: EventT<HTMLInputElement>) {
                                for (const row of rows.value) {
                                    row.selected.value = ev.currentTarget.checked;
                                }
                            }
                        })
                    ),
                    ...tableInfo.columns.map(col => th({ tabIndex: 0 }, col.name)
                    )
                )
            ),
            tbody(dynamic(rows, () => rows.value.map(({ selected, record }) => tr(
                td(
                    input({ type: 'checkbox' }, {
                        checked: selected.value,
                        onmount(ev: EventT<HTMLInputElement>) {
                            const currentTarget = ev.currentTarget;
                            selected.sub(currentTarget, () => {
                                currentTarget.checked = selected.value;
                            });
                        },
                        onchange(ev: EventT<HTMLInputElement>) {
                            selected.value = ev.currentTarget.checked;
                        }
                    })
                ),
                ...record.map((item, i) => {
                    const value = val(item);

                    value.sub(record, async () => {
                        // Update state
                        record[i] = value.value;
                    });

                    async function editValue(ev: Event): Promise<void> {
                        const newValue = await modalCell(tableInfo.columns[i], item);

                        if (typeof newValue !== 'undefined') {
                            const response = await updateRecords(databasePath, schema.name, tableInfo, pkColumns, i, record, newValue);
                            if (response.ok) {
                                value.value = newValue;
                            }
                            else {
                                alert(response.errorMessage || 'Something went wrong.');
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
                        dynamic(value, () => storeTypeToString(value.value, tableInfo.columns[i].storeType))
                    );
                })
            )
            )))
        )
    );
}

export async function modalCell<TValue extends Primitive | null>(column: Column, value: TValue): Promise<TValue | undefined> {

    let newValue = val(value);

    const confirmed = await modalConfirm(
        h2(column.name),
        valueEditor(column, newValue),
    );

    return confirmed ? newValue.value : undefined;
}

export function storeTypeToString(value: Primitive | null, storeType: StoreType): string {
    switch (storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + storeType);

        case StoreType.General:
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
            throw new Unreachable(storeType);
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

        case StoreType.General:
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
            throw new Unreachable(storeType);
    }
}

export function getNewRecordColumns(tableInfo: Table) {
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

export function createNewRecord(columns: Column[]) {
    return columns
        .map(column => stringToStoreType('', column.storeType, column.isNullable));
}

export function valueEditor(column: Column, value: Val<Primitive | null>) {
    switch (column.storeType) {
        case StoreType.Blob:
            throw new Error('Not implemented: ' + column.storeType);

        case StoreType.General:
        case StoreType.Text:
            return textarea({ class: 'value-editor-' + column.storeType }, {
                onchange(ev: EventT<HTMLTextAreaElement>) { value.value = stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable); }
            },
                storeTypeToString(value.value, column.storeType)
            );

        case StoreType.Boolean:
            return div({ class: 'value-editor-' + column.storeType },
                label(
                    input({ type: 'radio' }, {
                        value: 'true',
                        checked: value.value === true,
                        onchange() { value.value = true; }
                    }),
                    'Yes'
                ),
                label(
                    input({ type: 'radio' }, {
                        value: 'false',
                        checked: value.value !== true,
                        onchange() { value.value = false; }
                    }),
                    'No'
                )
            );

        case StoreType.Integer:
        case StoreType.Numeric:
        case StoreType.Real:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.value, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.value = stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable); }
            });

        case StoreType.Date:
        case StoreType.Time:
        case StoreType.Timestamp:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.value, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.value = stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable); }
            });

        case StoreType.Uuid:
            return input({ class: 'value-editor-' + column.storeType }, {
                value: storeTypeToString(value.value, column.storeType),
                onchange(ev: EventT<HTMLInputElement>) { value.value = stringToStoreType(ev.currentTarget.value, column.storeType, column.isNullable); }
            });

        default:
            throw new Unreachable(column.storeType);
    }
}
