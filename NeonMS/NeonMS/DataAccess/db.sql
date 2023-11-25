-- SCHEMA: db

-- DROP SCHEMA IF EXISTS db ;

CREATE SCHEMA IF NOT EXISTS db
    AUTHORIZATION pg_database_owner;

GRANT ALL ON SCHEMA db TO pg_database_owner;


-- View: db.meta

-- DROP VIEW db.meta;

CREATE OR REPLACE VIEW db.meta
 AS
 WITH "references" AS (
         SELECT rc.constraint_catalog,
            rc.constraint_schema,
            rc.constraint_name,
            kcu1.table_schema,
            kcu1.table_name,
            json_agg(kcu1.column_name ORDER BY kcu1.ordinal_position) AS columns,
            kcu2.table_schema AS referenced_table_schema,
            kcu2.table_name AS referenced_table_name,
            json_agg(kcu2.column_name ORDER BY kcu2.ordinal_position) AS referenced_columns,
            lower(rc.update_rule::text) AS update_rule,
            lower(rc.delete_rule::text) AS delete_rule
           FROM information_schema.referential_constraints rc
             JOIN information_schema.key_column_usage kcu1 USING (constraint_catalog, constraint_schema, constraint_name)
             JOIN information_schema.key_column_usage kcu2 ON kcu2.constraint_catalog::name = rc.unique_constraint_catalog::name AND kcu2.constraint_schema::name = rc.unique_constraint_schema::name AND kcu2.constraint_name::name = rc.unique_constraint_name::name AND kcu2.ordinal_position::integer = kcu1.ordinal_position::integer
          GROUP BY rc.constraint_catalog, rc.constraint_schema, rc.constraint_name, kcu1.table_schema, kcu1.table_name, kcu2.table_schema, kcu2.table_name, kcu2.column_name, rc.update_rule, rc.delete_rule
        ), columns AS (
         SELECT c.table_catalog AS catalog_name,
            c.table_schema AS schema_name,
            c.table_name,
            c.ordinal_position AS "position",
            c.column_name AS name,
            c.udt_name AS column_type,
                CASE
                    WHEN c.is_nullable::text = 'YES'::text THEN true
                    ELSE false
                END AS is_nullable,
            c.column_default,
            c.generation_expression,
                CASE
                    WHEN c.is_updatable::text = 'YES'::text THEN true
                    ELSE false
                END AS is_updatable
           FROM information_schema.columns c
        ), indexes AS (
         SELECT schema_ns.nspname AS schema_name,
            table_class.relname AS table_name,
            index_class.relname AS name,
                CASE
                    WHEN i.indisprimary THEN 'pk'::text
                    WHEN i.indisunique THEN 'uc'::text
                    ELSE 'i'::text
                END AS index_type,
            json_agg(c.column_name) AS columns
           FROM pg_namespace schema_ns
             JOIN pg_class index_class ON index_class.relnamespace = schema_ns.oid
             JOIN pg_index i ON i.indexrelid = index_class.oid
             JOIN pg_class table_class ON i.indrelid = table_class.oid
             JOIN information_schema.columns c ON c.table_schema::name = schema_ns.nspname AND c.table_name::name = table_class.relname AND (c.ordinal_position::integer = ANY (i.indkey::smallint[]))
          GROUP BY schema_ns.nspname, table_class.relname, index_class.relname, i.indisunique, i.indisprimary
        ), tables AS (
         SELECT st.table_catalog AS catalog_name,
            st.table_schema AS schema_name,
            st.table_name AS name,
            t.tableowner AS owner,
                CASE
                    WHEN st.is_insertable_into::text = 'YES'::text THEN true
                    ELSE false
                END AS is_insertable_into,
            COALESCE(c.columns, '[]'::json) AS columns,
            COALESCE(i.indexes, '[]'::json) AS indexes
           FROM information_schema.tables st
             JOIN pg_tables t ON st.table_schema::name = t.schemaname AND st.table_name::name = t.tablename
             LEFT JOIN ( SELECT c_1.catalog_name,
                    c_1.schema_name,
                    c_1.table_name,
                    json_agg(to_jsonb(c_1.*) - ARRAY['catalog_name'::text, 'schema_name'::text, 'table_name'::text] ORDER BY c_1."position", c_1.name) AS columns
                   FROM columns c_1
                  GROUP BY c_1.catalog_name, c_1.schema_name, c_1.table_name) c ON c.catalog_name::name = st.table_catalog::name AND c.schema_name::name = st.table_schema::name AND c.table_name::name = st.table_name::name
             LEFT JOIN ( SELECT i_1.schema_name,
                    i_1.table_name,
                    json_agg(to_jsonb(i_1.*) - ARRAY['catalog_name'::text, 'schema_name'::text, 'table_name'::text]) AS indexes
                   FROM indexes i_1
                  GROUP BY i_1.schema_name, i_1.table_name) i ON i.schema_name = st.table_schema::name AND i.table_name = st.table_name::name
        ), schemas AS (
         SELECT s.catalog_name,
            s.schema_name AS name,
            s.schema_owner AS owner,
            COALESCE(t.tables, '[]'::json) AS tables,
            COALESCE(r."references", '[]'::json) AS "references"
           FROM information_schema.schemata s
             LEFT JOIN ( SELECT t_1.catalog_name,
                    t_1.schema_name,
                    json_agg(to_jsonb(t_1.*) - ARRAY['catalog_name'::text, 'schema_name'::text] ORDER BY t_1.name) AS tables
                   FROM tables t_1
                  GROUP BY t_1.catalog_name, t_1.schema_name) t ON t.catalog_name::name = s.catalog_name::name AND t.schema_name::name = s.schema_name::name
             LEFT JOIN ( SELECT r_1.constraint_catalog,
                    r_1.constraint_schema,
                    json_agg(to_jsonb(r_1.*) - ARRAY['constraint_catalog'::text, 'constraint_schema'::text] ORDER BY r_1.constraint_name) AS "references"
                   FROM "references" r_1
                  GROUP BY r_1.constraint_catalog, r_1.constraint_schema) r ON r.constraint_catalog::name = s.catalog_name::name AND r.constraint_schema::name = s.schema_name::name
          WHERE s.schema_name::name <> 'information_schema'::name AND NOT starts_with(s.schema_name::name::text, 'pg_'::text)
        ), databases AS (
         SELECT d.catalog_name AS name,
            d.catalog_owner AS owner,
            COALESCE(s.schemas, '[]'::json) AS schemas
           FROM ( SELECT d_1.datname::text AS catalog_name,
                    d_1.datdba::regrole::text AS catalog_owner
                   FROM pg_database d_1
                  WHERE d_1.datname = CURRENT_CATALOG) d
             LEFT JOIN ( SELECT s_1.catalog_name,
                    json_agg(to_jsonb(s_1.*) - ARRAY['catalog_name'::text] ORDER BY 'Name'::text) AS schemas
                   FROM schemas s_1
                  GROUP BY s_1.catalog_name) s ON s.catalog_name::name = d.catalog_name
        )
 SELECT databases.name,
    databases.owner,
    databases.schemas
   FROM databases;

ALTER TABLE db.meta
    OWNER TO pg_database_owner;

GRANT ALL ON TABLE db.meta TO pg_database_owner;

