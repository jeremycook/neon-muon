import { FileNode, getParentPath } from '../files/files';
import { spreadsheet } from '../ui/spreadsheet';
import { lazy } from '../utils/dynamicHtml';
import { div, h1 } from '../utils/html';
import { getDatabase } from './database';
import { selectRecords } from './records';

export async function tableApp({ fileNode }: { fileNode: FileNode }) {
    const databasePath = getParentPath(fileNode.path);
    const database = (await getDatabase(databasePath))!;
    const schema = database.schemas[0];
    const tableInfo = schema.tables.find(t => t.name === fileNode.name)!;

    const response = await selectRecords(databasePath, schema.name, tableInfo.name, tableInfo.columns.map(col => col.name));
    const records = response.getResultOrThrow();

    return div({ class: 'flex flex-down fill' },
        div(
            h1(tableInfo.name),
        ),
        div({ class: 'flex flex-down flex-grow overflow-auto' },
            ...lazy(
                spreadsheet(records),
                div('Loading…')
            )
        )
    )
}