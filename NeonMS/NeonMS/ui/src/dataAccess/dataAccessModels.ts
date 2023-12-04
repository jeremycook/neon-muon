export interface Schema {
    name: string;
    owner: string;
    tables: Table[];
    references: Reference[];
}

export interface Reference {
    columns: string[];
    table_name: string;
    delete_rule: ReferenceRule;
    update_rule: ReferenceRule;
    table_schema: string;
    constraint_name: string;
    referenced_columns: string[];
    referenced_table_name: string;
    referenced_table_schema: string;
}

export enum ReferenceRule {
    Cascade = "cascade",
    Restrict = "restrict",
}

export interface Table {
    name: string;
    owner: string;
    columns: TableColumn[];
    indexes: Index[];
    is_insertable_into: boolean;
}

export interface TableColumn {
    name: string;
    position: number;
    column_type: ColumnType;
    is_nullable: boolean;
    is_updatable: boolean;
    column_default: null | string;
    generation_expression: null | string;
}

export enum ColumnType {
    Bool = "bool",
    Bytea = "bytea",
    Date = "date",
    Int4 = "int4",
    Numeric = "numeric",
    Text = "text",
    Timestamp = "timestamp",
    UUID = "uuid",
    Varchar = "varchar",
}

export interface Index {
    name: string;
    columns: string[];
    index_type: IndexType;
}

export enum IndexType {
    I = "i",
    Pk = "pk",
    Uc = "uc",
}