import { jsonPost, jsonPut } from "../utils/http";
import { Primitive } from "../utils/types";

export async function previewBatch(input: Readonly<QueryInput>) {
    return await jsonPut<Array<QueryResult>>('/api/query/batch', input);
}

export async function applyBatch(input: Readonly<QueryInput>) {
    return await jsonPost<Array<QueryResult>>('/api/query/batch', input);
}

export interface QueryInput {
    server?: string;
    database: string;
    actions: Array<QueryAction>;
}

export interface QueryAction {
    sql: string;
    columns?: boolean;
    page?: number;
    range?: number;
    parameters?: Record<string, null | Primitive | object>;
}

export interface QueryResult {
    columns: QueryColumn[];
    rows: Array<Array<null | boolean | Date | number | object | string>>;
}

export interface QueryColumn {
    dataTypeName: string;
    columnName: string;
    columnOrdinal: number;
    columnSize: null;
    allowDBNull: null;
    isAutoIncrement: null;
    isIdentity: boolean;
    isKey: null;
    isReadOnly: boolean;
    isUnique: null;
}
