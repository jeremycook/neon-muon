import { jsonPut } from '../utils/http';
import { Primitive } from './database';


export async function selectRecords(database: string, schema: string, table: string, columns: string[]) {
    const url = '/api/select-records';
    const data = {
        database,
        schema,
        table,
        columns,
    };
    const response = await jsonPut<(Primitive | null)[][]>(url, data);
    return response.result ?? [];
}
