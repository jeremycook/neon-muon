import { FileNode, getParentPath } from '../files/files';
import { ColumnProp, SpreadsheetChangeTracker, spreadsheet } from '../ui/spreadsheet';
import { lazy, when } from '../utils/dynamicHtml';
import { button, div, h1 } from '../utils/html';
import { val } from '../utils/pubSub';
import { Schema, Table, getDatabase } from './database';
import { selectRecords } from './records';

export async function tableApp({ fileNode }: { fileNode: FileNode }) {
    const databasePath = getParentPath(fileNode.path);
    const database = (await getDatabase(databasePath))!;
    const schema = database.schemas[0];
    const tableInfo = schema.tables.find(t => t.name === fileNode.name)!;

    const columns: ColumnProp[] = tableInfo.columns.map(column => ({
        label: column.name,
    }));

    const hasChanges = val(false);
    const changeTracker = new SpreadsheetChangeTracker();
    changeTracker.addEventListener(() => hasChanges.pub(true));

    return div({ class: 'flex flex-down fill' },
        div(
            h1(tableInfo.name),
        ),
        div({ class: 'flex gap mb' },
            when(hasChanges,
                () => button({ class: 'button' }, 'Save Changes'),
                () => button({ class: 'button', disabled: true }, 'Save Changes')
            )
        ),
        div({ class: 'flex flex-down flex-grow overflow-auto' },
            ...lazy(
                async () => spreadsheet({
                    columns,
                    records: await getRecords(databasePath, schema, tableInfo),
                    onChange(change) {
                        changeTracker.push(change);
                    },
                }),
                div('Loading…')
            )
        )
    )
}

async function getRecords(databasePath: string, schema: Schema, tableInfo: Table) {
    const response = await selectRecords(databasePath, schema.name, tableInfo.name, tableInfo.columns.map(col => col.name));
    const records = response.getResultOrThrow();
    return records;
}
