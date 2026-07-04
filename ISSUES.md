# Sqlil Issues

## Critical

- [x] `IS NULL` / `IS NOT NULL` — null comparisons generate `= NULL` instead of `IS NULL`
- [x] Chained `.Where()` fails — `.Where(A).Where(B)` throws
- [x] `Select` discards ORDER BY — `.OrderBy(x).Select(x)` loses ordering
- [x] Nullable materialization crash — `Convert.ChangeType(null, ...)` throws
- [x] `ExpressionNotSupportedException` precedence bug — mangled messages
- [x] `Constant` only handles primitives/strings — Guid, DateTime, etc. fail
- [x] Wrong aggregate return types — Count always long, Sum/Average always double
- [x] Input params hardcoded to `true`

## Missing LINQ Methods

- [x] `Distinct()`
- [x] `ThenBy()` / `ThenByDescending()`
- [x] `GroupBy()`
- [x] `FirstOrDefault()` / `Any()` / `Single()` / `Last()`
- [x] `Concat()` / `Union()` / `Intersect()` / `Except()`
- [x] `ValueTuple.Create` in SELECT
- [x] Aggregate 1-argument forms

## Missing SQL Features

- [x] `CASE WHEN ... THEN ... END`
- [x] `IN (subquery)` / `IN (value_list)` (AST + composer only)
- [x] `COALESCE` / null-coalescing `??`
- [x] `CAST(expr AS type)` (AST + composer only)
- [x] `BETWEEN` (AST + composer only)
- [x] `RETURNING` clause for DML
- [x] `INSERT ... ON CONFLICT` (upsert)
- [x] Modulo `%`
- [x] Bitwise operators (`&`, `|`, `~`, `<<`, `>>`)
- [ ] Correlated subqueries (outer parameter in inner predicate)

## Test Coverage

- [x] NULL comparison tests
- [x] Sum/Average tests
- [x] Subquery tests (basic EXISTS, correlated subqueries documented as limitation)
- [x] GroupJoin tests
- [x] SelectAnonymous tests
- [x] ThenBy tests
- [x] Arithmetic tests (modulo, CASE WHEN)
- [x] Extract shared test helpers

## Cleanup

- [x] Make `SqliteComposer` static in `DbConnectionExtensions`
- [x] Extract duplicate materialization logic
- [x] Remove dead `Sqlil` project code
