import { jsonPost, jsonPut } from '../utils/http';
import { Column, Primitive, Table } from './database';

const selectRecordsUrl = '/api/select-records';
const insertRecordsUrl = '/api/insert-records';
const updateRecordsUrl = '/api/update-records';
const deleteRecordsUrl = '/api/delete-records';

export async function selectRecords(database: string, schema: string, table: string, columns: string[]) {
    const input = {
        database,
        schema,
        table,
        columns,
    };
    return await jsonPut<(Primitive | null)[][]>(selectRecordsUrl, input);
}

export async function insertRecords(databasePath: string, schema: string, table: string, columns: string[], returningColumns: string[], newRecords: (Primitive | null)[][]) {
    const input = {
        database: databasePath,
        schema,
        table,
        columns,
        records: newRecords,
        returningColumns,
    };
    return await jsonPost<(Primitive | null)[][]>(insertRecordsUrl, input);
}

export async function updateRecords(databasePath: string, schema: string, tableInfo: Table, pkColumns: number[], i: number, record: (Primitive | null)[], newValue: Primitive | null) {
    const input = {
        database: databasePath,
        schema,
        table: tableInfo.name,
        columns: buildTableModificationColumns(tableInfo.columns, pkColumns, i),
        records: [buildTableModificationRecord(record, pkColumns, newValue)]
    };
    return await jsonPost(updateRecordsUrl, input);
}

export async function deleteRecords(databasePath: string, schema: string, table: string, columns: string[], records: (Primitive | null)[][]) {
    const input = {
        database: databasePath,
        schema,
        table,
        columns,
        records,
    };
    return await jsonPost(deleteRecordsUrl, input);
}

function buildTableModificationColumns(columns: Column[], pkColumns: number[], ...valueColumns: number[]) {
    return pkColumns.concat(valueColumns).map(i => columns[i].name);
}

function buildTableModificationRecord(record: (Primitive | null)[], pkColumns: number[], ...value: (Primitive | null)[]) {
    return [...pkColumns.map(i => record[i]), ...value];
}
