# Sqlil Issues

## Critical

- [ ] `IS NULL` / `IS NOT NULL` — null comparisons generate `= NULL` instead of `IS NULL`
- [ ] Chained `.Where()` fails — `.Where(A).Where(B)` throws
- [ ] `Select` discards ORDER BY — `.OrderBy(x).Select(x)` loses ordering
- [ ] Nullable materialization crash — `Convert.ChangeType(null, ...)` throws
- [ ] `ExpressionNotSupportedException` precedence bug — mangled messages
- [ ] `Constant` only handles primitives/strings — Guid, DateTime, etc. fail
- [ ] Wrong aggregate return types — Count always long, Sum/Average always double
- [ ] Input params hardcoded to `true`

## Missing LINQ Methods

- [ ] `Distinct()`
- [ ] `ThenBy()` / `ThenByDescending()`
- [ ] `GroupBy()`
- [ ] `FirstOrDefault()` / `Any()` / `Single()` / `Last()`
- [ ] `Concat()` / `Union()` / `Intersect()` / `Except()`
- [ ] `ValueTuple.Create` in SELECT
- [ ] Aggregate 1-argument forms

## Missing SQL Features

- [ ] `CASE WHEN ... THEN ... END`
- [ ] `IN (subquery)` / `IN (value_list)`
- [ ] `COALESCE` / null-coalescing `??`
- [ ] `CAST(expr AS type)`
- [ ] `BETWEEN`
- [ ] `RETURNING` clause for DML
- [ ] `INSERT ... ON CONFLICT` (upsert)
- [ ] Modulo `%`, bitwise operators

## Test Coverage

- [ ] NULL comparison tests
- [ ] Sum/Average tests
- [ ] Subquery tests
- [ ] GroupJoin tests
- [ ] SelectAnonymous tests
- [ ] ThenBy tests
- [ ] Arithmetic tests
- [ ] Extract shared test helpers

## Cleanup

- [ ] Make `SqliteComposer` static in `DbConnectionExtensions`
- [ ] Extract duplicate materialization logic
- [ ] Remove dead `Sqlil` project code
