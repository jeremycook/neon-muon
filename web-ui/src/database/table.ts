import { FileNode, getParentPath } from '../files/files';
import { lazy } from '../utils/dynamicHtml';
import { div, h1 } from '../utils/html';
import { getDatabase } from './database';
import { databaseTable } from './databaseTable';

export async function tableApp({ fileNode }: { fileNode: FileNode }) {
    const databasePath = getParentPath(fileNode.path);
    const database = (await getDatabase(databasePath))!;
    const schema = database.schemas[0];
    const tableInfo = schema.tables.find(t => t.name === fileNode.name)!;

    return div(
        h1(tableInfo.name),
        ...lazy(
            databaseTable(tableInfo, schema, databasePath),
            div('Loading…')
        )
    )
}