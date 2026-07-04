using Sqlil.Core.Syntax;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlil.Core.ExpressionTranslation;

public partial class SelectStmtTranslator {
    protected virtual object Call(MethodCallExpression expression, TranslationContext context) {

        if (expression.Method.DeclaringType == typeof(Queryable)) {
            object result = expression.Method.Name switch {
                nameof(Queryable.Select) => Select(expression, context),
                nameof(Queryable.Join) => Join(expression, context),
                nameof(Queryable.GroupJoin) => GroupJoin(expression, context),
                nameof(Queryable.Where) => Where(expression, context),
                nameof(Queryable.Distinct) => Distinct(expression, context),
                nameof(Queryable.OrderBy) => OrderBy(expression, context),
                nameof(Queryable.OrderByDescending) => OrderByDescending(expression, context),
                nameof(Queryable.ThenBy) => ThenBy(expression, context),
                nameof(Queryable.ThenByDescending) => ThenByDescending(expression, context),
                nameof(Queryable.Skip) => Skip(expression, context),
                nameof(Queryable.Take) => Take(expression, context),
                nameof(Queryable.Count) => Count(expression, context),
                nameof(Queryable.LongCount) => Count(expression, context),
                nameof(Queryable.Any) => Any(expression, context),
                nameof(Queryable.First) => First(expression, context),
                nameof(Queryable.FirstOrDefault) => First(expression, context),
                nameof(Queryable.Single) => Single(expression, context),
                nameof(Queryable.SingleOrDefault) => Single(expression, context),
                nameof(Queryable.Sum) => Aggregate(expression, context, ExprFunctionName.Sum),
                nameof(Queryable.Average) => Aggregate(expression, context, ExprFunctionName.Average),
                nameof(Queryable.Min) => Aggregate(expression, context, ExprFunctionName.Min),
                nameof(Queryable.Max) => Aggregate(expression, context, ExprFunctionName.Max),
                nameof(Queryable.Union) => Compound(expression, context, CompoundOperator.Union),
                nameof(Queryable.Concat) => Compound(expression, context, CompoundOperator.UnionAll),
                nameof(Queryable.Intersect) => Compound(expression, context, CompoundOperator.Intersect),
                nameof(Queryable.Except) => Compound(expression, context, CompoundOperator.Except),
                nameof(Queryable.GroupBy) => GroupBy(expression, context),
                _ => throw new ExpressionNotSupportedException($"Method not supported {expression.Method}.", expression),
            };
            return result;
        }

        else if (expression.Method.DeclaringType == typeof(Core.QueryableExtensions)) {
            object result = expression.Method.Name switch {
                nameof(Core.QueryableExtensions.Insert) => Insert(expression, context),
                nameof(Core.QueryableExtensions.Update) => Update(expression, context),
                nameof(Core.QueryableExtensions.Delete) => Delete(expression, context),
                _ => throw new ExpressionNotSupportedException($"Method not supported {expression.Method}.", expression),
            };
            return result;
        }

        else if (expression.Method.DeclaringType == typeof(ValueTuple)) {
            var result = expression.Method.Name switch {
                nameof(ValueTuple.Create) => CreateTuple(expression, context),
                _ => throw new ExpressionNotSupportedException($"Method not supported {expression.Method}.", expression),
            };
            return result;
        }

        else if (expression.Method.DeclaringType == typeof(string)) {
            Expr result = TranslateStringMethod(expression, context);
            return result;
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }

    protected virtual Expr TranslateStringMethod(MethodCallExpression expression, TranslationContext context) {
        Expr result = expression.Method.Name switch {

            nameof(string.Contains) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)Translate(expression.Object!, context),
                ExprBinary.Create(BinaryOperator.Concat, ExprLiteralString.Create("%"), ExprBinary.Create(BinaryOperator.Concat, (Expr)Translate(expression.Arguments[0], context), ExprLiteralString.Create("%")))
            ),

            nameof(string.StartsWith) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)Translate(expression.Object!, context),
                ExprBinary.Create(BinaryOperator.Concat, (Expr)Translate(expression.Arguments[0], context), ExprLiteralString.Create("%"))
            ),

            nameof(string.EndsWith) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)Translate(expression.Object!, context),
                ExprBinary.Create(BinaryOperator.Concat, ExprLiteralString.Create("%"), (Expr)Translate(expression.Arguments[0], context))
            ),

            nameof(string.ToLower) => ExprFunction.Create(ExprFunctionName.Lower, (Expr)Translate(expression.Object!, context)),
            nameof(string.ToUpper) => ExprFunction.Create(ExprFunctionName.Upper, (Expr)Translate(expression.Object!, context)),

            _ => throw new ExpressionNotSupportedException($"Method not supported {expression.Method}.", expression),
        };
        return result;
    }

    protected virtual SelectCoreNormal Where(MethodCallExpression expression, TranslationContext context) {

        if (expression.Arguments.Count == 2) {

            // IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            var source = Translate(expression.Arguments[0], context);
            var predicate = Translate(expression.Arguments[1], context);
            var expr = ExtractExpr(predicate);

            if (expr is not null) {

                if (source is TableOrSubquery tableOrSubquery) {
                    return SelectCoreNormal.Create(tableOrSubquery, Where: expr);
                }

                if (source is SelectStmt selectStmt &&
                    selectStmt.SelectCores.Count == 1 &&
                    selectStmt.SelectCores[0] is SelectCoreNormal existingCore) {
                    // Merge WHERE conditions with AND
                    var mergedWhere = existingCore.Where != null
                        ? ExprBinary.Create(BinaryOperator.AndAlso, existingCore.Where, expr)
                        : expr;
                    return existingCore with { Where = mergedWhere };
                }

                if (source is SelectCoreNormal selectCore) {
                    var mergedWhere = selectCore.Where != null
                        ? ExprBinary.Create(BinaryOperator.AndAlso, selectCore.Where, expr)
                        : expr;
                    return selectCore with { Where = mergedWhere };
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    /// <summary>
    /// Supported: <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>
    /// Not Supported: <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, int, TResult}})"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public virtual SelectStmt Select(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {

            // IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
            var currentContext = context with {
                ParameterName = GetTableName(expression.Arguments[1]),
            };
            var source = Translate(expression.Arguments[0], currentContext);

            // Check if source is grouped (has non-empty GroupBys)
            var groupedCore = GetGroupedCore(source);
            if (groupedCore != null && groupedCore.GroupBys.Any()) {
                // Translate selector in grouping context
                var selectorLambda = UnwrapLambda(expression.Arguments[1]);
                if (selectorLambda == null) {
                    throw new ExpressionNotSupportedException("Select selector must be a lambda.", expression);
                }
                return TranslateGroupByResultSelector(source, selectorLambda, groupedCore.GroupBys, expression);
            }

            var selector = Translate(expression.Arguments[1], currentContext);

            if (source is SelectStmt selectStmt) {

                if (selectStmt.SelectCores.Count == 1 &&
                    selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {

                    // Replace result columns regardless of what they currently are
                    var result = selector switch {

                        StableList<ResultColumn> resultColumnList => selectStmt with {
                            SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                                ResultColumns = resultColumnList,
                            })
                        },

                        StableList<Expr> exprList => selectStmt with {
                            SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                                ResultColumns = StableList.Create<ResultColumn>(exprList.Select(e => ResultColumnExpr.Create(e)).ToArray()),
                            })
                        },

                        Expr expr => selectStmt with {
                            SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                                ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(expr)),
                            })
                        },

                        _ => throw new ExpressionNotSupportedException($"Selector not supported {selector.GetType()}: {selector}.", expression),
                    };
                    return result;
                }
            }

            else if (source is SelectCoreNormal selectCoreNormal) {

                var result = selector switch {

                    StableList<ResultColumn> resultColumnList => SelectStmt.Create(
                        selectCoreNormal with {
                            ResultColumns = resultColumnList,
                        }
                    ),

                    StableList<Expr> exprList => SelectStmt.Create(
                        selectCoreNormal with {
                            ResultColumns = StableList.Create<ResultColumn>(exprList.Select(e => ResultColumnExpr.Create(e)).ToArray()),
                        }
                    ),

                    Expr expr => SelectStmt.Create(
                        selectCoreNormal with {
                            ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(expr)),
                        }
                    ),

                    _ => throw new ExpressionNotSupportedException($"Selector not supported {selector.GetType()}: {selector}.", expression),
                };
                return result;
            }

            else if (source is TableOrSubquery tableOrSubquery) {
                var result = selector switch {
                    Expr expr => SelectStmt.Create(SelectCoreNormal.Create(StableList.Create<ResultColumn>(ResultColumnExpr.Create(expr)), tableOrSubquery)),
                    ResultColumn resultColumn => SelectStmt.Create(SelectCoreNormal.Create(StableList.Create(resultColumn), tableOrSubquery)),
                    StableList<ResultColumn> resultColumnList => SelectStmt.Create(SelectCoreNormal.Create(resultColumnList, tableOrSubquery)),
                    _ => throw new ExpressionNotSupportedException($"Selector not supported {selector.GetType()}: {selector}.", expression),
                };
                return result;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    /// <summary>
    /// IQueryable&lt;TResult&gt; Join&lt;TOuter, TInner, TKey, TResult&gt;(
    ///     this IQueryable&lt;TOuter&gt; outer,
    ///     IEnumerable&lt;TInner&gt; inner,
    ///     Expression&lt;Func&lt;TOuter, TKey&gt;&gt; outerKeySelector,
    ///     Expression&lt;Func&lt;TInner, TKey&gt;&gt; innerKeySelector,
    ///     Expression&lt;Func&lt;TOuter, TInner, TResult&gt;&gt; resultSelector)
    /// </summary>
    protected virtual SelectStmt Join(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 5) {
            // IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(...)
            // The lambda parameter names (e.g., u, ur) serve as table aliases
            var outerKeyLambda = UnwrapLambda(expression.Arguments[2]);
            var innerKeyLambda = UnwrapLambda(expression.Arguments[3]);
            var resultLambda = UnwrapLambda(expression.Arguments[4]);

            var outerAlias = outerKeyLambda?.Parameters.Count == 1
                ? TableName.Create(outerKeyLambda.Parameters[0].Name ?? string.Empty, outerKeyLambda.Parameters[0].Type)
                : null;
            var innerAlias = innerKeyLambda?.Parameters.Count == 1
                ? TableName.Create(innerKeyLambda.Parameters[0].Name ?? string.Empty, innerKeyLambda.Parameters[0].Type)
                : null;

            var outer = Translate(expression.Arguments[0], context);
            var inner = Translate(expression.Arguments[1], context);

            // Apply table aliases from the key selector lambda parameters
            if (outer is TableOrSubqueryTable outerTableRaw && outerAlias != null) {
                outer = outerTableRaw with { TableAlias = outerAlias };
            }
            if (inner is TableOrSubqueryTable innerTableRaw && innerAlias != null) {
                inner = innerTableRaw with { TableAlias = innerAlias };
            }

            var outerCtx = context with { ParameterName = outerAlias };
            var outerKeySelector = Translate(expression.Arguments[2], outerCtx);
            var innerKeySelector = Translate(expression.Arguments[3], context with { ParameterName = innerAlias });
            var resultCtx = resultLambda?.Parameters.Count == 1
                ? context with {
                    ParameterName = TableName.Create(resultLambda.Parameters[0].Name ?? string.Empty, resultLambda.Parameters[0].Type)
                }
                : context;
            var resultSelector = Translate(expression.Arguments[4], resultCtx);

            if (outer is TableOrSubquery outerTable &&
                inner is TableOrSubquery innerTable &&
                outerKeySelector is Expr outerKey &&
                innerKeySelector is Expr innerKey) {

                var joinConstraint = JoinConstraintOn.Create(
                    ExprBinary.Create(BinaryOperator.Equal, outerKey, innerKey)
                );

                var joinClause = JoinClause.Create(
                    outerTable,
                    (JoinOperator.Inner, innerTable, (JoinConstraint)joinConstraint)
                );

                var resultColumns = resultSelector switch {
                    StableList<ResultColumn> resultColumnList => resultColumnList,
                    Expr expr => StableList.Create<ResultColumn>(ResultColumnExpr.Create(expr)),
                    _ => throw new ExpressionNotSupportedException($"Join result selector not supported: {resultSelector.GetType()}.", expression)
                };

                var selectCore = new SelectCoreNormal(
                    Distinct: false,
                    ResultColumns: resultColumns,
                    TableOrSubqueries: StableList<TableOrSubquery>.Empty,
                    JoinClause: joinClause,
                    Where: null,
                    GroupBys: StableList<Expr>.Empty,
                    Having: null,
                    Windows: StableList<(string, WindowDefn)>.Empty
                );

                return SelectStmt.Create(selectCore);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    /// <summary>
    /// IQueryable&lt;TResult&gt; GroupJoin&lt;TOuter, TInner, TKey, TResult&gt;(
    ///     this IQueryable&lt;TOuter&gt; outer,
    ///     IEnumerable&lt;TInner&gt; inner,
    ///     Expression&lt;Func&lt;TOuter, TKey&gt;&gt; outerKeySelector,
    ///     Expression&lt;Func&lt;TInner, TKey&gt;&gt; innerKeySelector,
    ///     Expression&lt;Func&lt;TOuter, IEnumerable&lt;TInner&gt;, TResult&gt;&gt; resultSelector)
    /// 
    /// Translates to a LEFT JOIN with the inner table as a subquery, producing a correlated subquery for the group.
    /// </summary>
    protected virtual SelectStmt GroupJoin(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 5) {
            var outer = Translate(expression.Arguments[0], context);
            var inner = Translate(expression.Arguments[1], context);
            var outerKeySelector = Translate(expression.Arguments[2], context);
            var innerKeySelector = Translate(expression.Arguments[3], context);
            var resultSelector = Translate(expression.Arguments[4], context);

            if (outer is TableOrSubquery outerTable &&
                inner is TableOrSubquery innerTable &&
                outerKeySelector is Expr outerKey &&
                innerKeySelector is Expr innerKey) {

                // For GroupJoin, the result selector is: (outer, innerGroup) => result
                // We need to produce: SELECT resultColumns FROM outer LEFT JOIN (
                //   SELECT inner.* FROM inner GROUP BY inner.*
                // ) ON outerKey = innerKey

                // Build a subquery for the inner table
                var innerSubquery = SelectStmt.Create(
                    SelectCoreNormal.Create(innerTable)
                );

                var joinConstraint = JoinConstraintOn.Create(
                    ExprBinary.Create(BinaryOperator.Equal, outerKey, innerKey)
                );

                var innerType = expression.Method.GetGenericArguments()[1];
                var subqueryTable = TableOrSubquerySelectStmts.Create(
                    StableList.Create(innerSubquery),
                    TableAlias: TableName.Create("innerGroup", innerType)
                );

                var joinClause = JoinClause.Create(
                    outerTable,
                    (JoinOperator.Left, subqueryTable, (JoinConstraint)joinConstraint)
                );

                // Build result columns from the result selector
                StableList<ResultColumn> resultColumns;
                if (resultSelector is LambdaExpression lambda &&
                    lambda.Body is NewExpression newExpr) {
                    resultColumns = StableList.Create<ResultColumn>(
                        newExpr.Arguments
                            .Select((arg, i) => {
                                var translated = Translate(arg, context);
                                return translated switch {
                                    Expr expr => ResultColumnExpr.Create(expr, ColumnName.Create(newExpr.Members![i].Name, GetMemberType(newExpr.Members[i]))),
                                    StableList<ResultColumn> cols => cols[0],
                                    _ => throw new ExpressionNotSupportedException($"GroupJoin result selector element not supported: {translated.GetType()}.", expression)
                                };
                            })
                            .ToArray()
                    );
                }
                else if (resultSelector is StableList<ResultColumn> resultColumnList) {
                    resultColumns = resultColumnList;
                }
                else if (resultSelector is Expr resultExpr) {
                    resultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(resultExpr));
                }
                else {
                    throw new ExpressionNotSupportedException($"GroupJoin result selector not supported: {resultSelector.GetType()}.", expression);
                }

                var selectCore = new SelectCoreNormal(
                    Distinct: false,
                    ResultColumns: resultColumns,
                    TableOrSubqueries: StableList<TableOrSubquery>.Empty,
                    JoinClause: joinClause,
                    Where: null,
                    GroupBys: StableList<Expr>.Empty,
                    Having: null,
                    Windows: StableList<(string, WindowDefn)>.Empty
                );

                return SelectStmt.Create(selectCore);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    /// <summary>
    /// Supported: <see cref="Queryable.OrderBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    protected virtual SelectStmt OrderBy(MethodCallExpression expression, TranslationContext context) {

        if (expression.Arguments.Count == 2) {

            // IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            var source = Translate(expression.Arguments[0], context);
            var keySelector = Translate(expression.Arguments[1], context);

            if (source is SelectCoreNormal selectCoreNormal) {

                if (keySelector is Expr expr) {

                    var result = SelectStmt.Create(
                        selectCoreNormal,
                        OrderingTerms: StableList.Create(OrderingTerm.Create(expr))
                    );
                    return result;
                }

                throw new ExpressionNotSupportedException(expression);
            }

            if (source is TableOrSubquery tableOrSubquery) {

                if (keySelector is Expr expr) {

                    var result = SelectStmt.Create(
                        SelectCoreNormal.Create(tableOrSubquery),
                        OrderingTerms: StableList.Create(OrderingTerm.Create(expr))
                    );
                    return result;
                }

                throw new ExpressionNotSupportedException(expression);
            }

            else {
                throw new ExpressionNotSupportedException(expression);
            }
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }

    /// <summary>
    /// Supported: <see cref="Queryable.OrderByDescending{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    protected virtual SelectStmt OrderByDescending(MethodCallExpression expression, TranslationContext context) {
        var orderBy = OrderBy(expression, context);

        var result = orderBy with {
            OrderingTerms = orderBy.OrderingTerms.Select(term => term with { Desc = true }).ToStableList()
        };
        return result;
    }

    protected virtual SelectStmt Skip(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {
            // IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count);
            var source = Translate(expression.Arguments[0], context);
            var count = Translate(expression.Arguments[1], context);

            if (source is SelectStmt selectStmt &&
                count is Expr expr) {

                var result = selectStmt with {
                    Offset = expr,
                };
                return result;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }

    protected virtual SelectStmt Take(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {
            // IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count);
            var source = Translate(expression.Arguments[0], context);
            var count = Translate(expression.Arguments[1], context);

            if (source is SelectStmt selectStmt &&
                count is Expr expr) {

                var result = selectStmt with {
                    Limit = expr,
                };
                return result;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }

    protected virtual object CreateTuple(MethodCallExpression expression, TranslationContext context) {
        // ValueTuple.Create(arg1, arg2, ...) → list of result columns
        var args = expression.Arguments.Select(arg => {
            var translated = Translate(arg, context);
            return translated switch {
                Expr expr => ResultColumnExpr.Create(expr),
                StableList<ResultColumn> cols => cols[0],
                _ => throw new ExpressionNotSupportedException($"Tuple element not supported: {translated.GetType()}.", expression)
            };
        }).ToArray();

        return StableList.Create(args);
    }

    /// <summary>
    /// Translates Queryable.Count() or Queryable.Count(predicate).
    /// </summary>
    protected virtual SelectStmt Count(MethodCallExpression expression, TranslationContext context) {
        // Determine if this is Count() or LongCount()
        var isLongCount = expression.Method.Name == nameof(Queryable.LongCount);
        var returnType = isLongCount ? typeof(long) : typeof(int);

        if (expression.Arguments.Count == 1) {
            // IQueryable<int> Count<TSource>(this IQueryable<TSource> source)
            var source = Translate(expression.Arguments[0], context);
            if (source is TableOrSubquery tableOrSubquery) {
                var aggregate = ExprFunction.Create(ExprFunctionName.Count, returnType);
                var selectCore = SelectCoreNormal.Create(tableOrSubquery);
                var result = selectCore with {
                    ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregate))
                };
                return SelectStmt.Create(result);
            }
            if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                var aggregate = ExprFunction.Create(ExprFunctionName.Count, returnType);
                var result = selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                        ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregate)),
                    })
                };
                return result;
            }
            throw new ExpressionNotSupportedException(expression);
        }
        else if (expression.Arguments.Count == 2) {
            // IQueryable<int> Count<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            var source = Translate(expression.Arguments[0], context);
            var predicate = Translate(expression.Arguments[1], context);

            if (source is TableOrSubquery tableOrSubquery && predicate is Expr expr) {
                var aggregate = ExprFunction.Create(ExprFunctionName.Count, returnType);
                var selectCore = SelectCoreNormal.Create(tableOrSubquery, Where: expr);
                var result = selectCore with {
                    ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregate))
                };
                return SelectStmt.Create(result);
            }
            throw new ExpressionNotSupportedException(expression);
        }
        throw new ExpressionNotSupportedException(expression);
    }

    /// <summary>
    /// Translates Queryable.Sum/Average/Min/Max with a selector.
    /// </summary>
    protected virtual SelectStmt Aggregate(MethodCallExpression expression, TranslationContext context, ExprFunctionName functionName) {
        // 1-argument form: Sum(source) without selector
        if (expression.Arguments.Count == 1) {
            var source = Translate(expression.Arguments[0], context);
            var aggregateExpr = ExprFunction.Create(functionName);

            if (source is TableOrSubquery tableOrSubquery) {
                var selectCore = SelectCoreNormal.Create(tableOrSubquery);
                var result = selectCore with {
                    ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregateExpr))
                };
                return SelectStmt.Create(result);
            }
            if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                var result = selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                        ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregateExpr)),
                    })
                };
                return result;
            }
            throw new ExpressionNotSupportedException(expression);
        }

        // 2-argument form: Sum(source, selector)
        if (expression.Arguments.Count == 2) {
            // IQueryable<TResult> Sum<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
            var currentContext = context with {
                ParameterName = GetTableName(expression.Arguments[1]),
            };
            var source = Translate(expression.Arguments[0], currentContext);
            var selector = Translate(expression.Arguments[1], currentContext);

            Expr aggregateExpr;
            if (selector is Expr expr) {
                aggregateExpr = ExprFunction.Create(functionName, expr);
            }
            else {
                throw new ExpressionNotSupportedException($"Aggregate selector not supported: {selector.GetType()}.", expression);
            }

            if (source is TableOrSubquery tableOrSubquery) {
                var selectCore = SelectCoreNormal.Create(tableOrSubquery);
                var result = selectCore with {
                    ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregateExpr))
                };
                return SelectStmt.Create(result);
            }
            if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                var result = selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                        ResultColumns = StableList.Create<ResultColumn>(ResultColumnExpr.Create(aggregateExpr)),
                    })
                };
                return result;
            }
            throw new ExpressionNotSupportedException(expression);
        }
        throw new ExpressionNotSupportedException(expression);
    }

    protected virtual SelectStmt Distinct(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 1) {
            var source = Translate(expression.Arguments[0], context);

            if (source is SelectCoreNormal core) {
                return SelectStmt.Create(core with { Distinct = true });
            }

            if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                return selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with { Distinct = true })
                };
            }

            if (source is TableOrSubquery tableOrSubquery) {
                return SelectStmt.Create(SelectCoreNormal.Create(tableOrSubquery) with { Distinct = true });
            }
        }
        throw new ExpressionNotSupportedException(expression);
    }

    protected virtual SelectStmt ThenBy(MethodCallExpression expression, TranslationContext context) {
        return AppendOrdering(expression, context, Desc: false);
    }

    protected virtual SelectStmt ThenByDescending(MethodCallExpression expression, TranslationContext context) {
        return AppendOrdering(expression, context, Desc: true);
    }

    private SelectStmt AppendOrdering(MethodCallExpression expression, TranslationContext context, bool Desc) {
        if (expression.Arguments.Count == 2) {
            var source = Translate(expression.Arguments[0], context);
            var keySelector = Translate(expression.Arguments[1], context);

            if (keySelector is Expr expr) {
                var orderingTerm = Desc
                    ? OrderingTerm.Create(expr, Desc: true)
                    : OrderingTerm.Create(expr);

                if (source is SelectStmt selectStmt) {
                    var newOrderingTerms = selectStmt.OrderingTerms.Append(orderingTerm).ToStableList();
                    return selectStmt with { OrderingTerms = newOrderingTerms };
                }
            }
        }
        throw new ExpressionNotSupportedException(expression);
    }

    protected virtual SelectStmt Any(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 1) {
            var source = Translate(expression.Arguments[0], context);
            return WrapInExists(source, expression);
        }
        if (expression.Arguments.Count == 2) {
            var source = Translate(expression.Arguments[0], context);

            // Build inner context with outer parameter scope for correlated subqueries
            var innerLambda = UnwrapLambda(expression.Arguments[1]);
            var innerParam = innerLambda?.Parameters[0];
            var innerContext = context;

            if (innerParam is not null && context.ParameterName is not null) {
                var outerParam = FindOuterParameterExpression(expression.Arguments[0]);
                if (outerParam is not null) {
                    var outerParams = context.OuterParameters is not null
                        ? new Dictionary<ParameterExpression, TableName>(context.OuterParameters)
                        : new Dictionary<ParameterExpression, TableName>();
                    outerParams[outerParam] = context.ParameterName;

                    innerContext = new TranslationContext(
                        ParameterName: GetTableName(innerParam),
                        OuterParameters: outerParams
                    );
                }
            }

            var predicate = Translate(expression.Arguments[1], innerContext);

            if (predicate is Expr expr) {
                // Apply predicate as WHERE then wrap in EXISTS
                if (source is TableOrSubquery tableOrSubquery) {
                    var core = SelectCoreNormal.Create(tableOrSubquery, Where: expr);
                    return WrapInExists(SelectStmt.Create(core), expression);
                }
                if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                    var mergedWhere = selectCoreNormal.Where != null
                        ? ExprBinary.Create(BinaryOperator.AndAlso, selectCoreNormal.Where, expr)
                        : expr;
                    return WrapInExists(selectStmt with {
                        SelectCores = StableList.Create<SelectCore>(selectCoreNormal with { Where = mergedWhere })
                    }, expression);
                }
            }
        }
        throw new ExpressionNotSupportedException(expression);
    }

    private SelectStmt WrapInExists(object source, MethodCallExpression expression) {
        SelectStmt innerStmt = source switch {
            SelectStmt s => s,
            SelectCoreNormal core => SelectStmt.Create(core),
            TableOrSubquery table => SelectStmt.Create(SelectCoreNormal.Create(table)),
            _ => throw new ExpressionNotSupportedException(expression)
        };

        var existsExpr = ExprExists.Create(innerStmt);
        var selectCore = SelectCoreNormal.Create(
            ResultColumns: StableList.Create<ResultColumn>(ResultColumnExpr.Create(existsExpr))
        );
        return SelectStmt.Create(selectCore);
    }

    protected virtual SelectStmt First(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 1) {
            var source = Translate(expression.Arguments[0], context);
            return ApplyLimit(source, 1, expression);
        }
        if (expression.Arguments.Count == 2) {
            var source = Translate(expression.Arguments[0], context);

            // Build inner context with outer parameter scope for correlated subqueries
            var innerLambda = UnwrapLambda(expression.Arguments[1]);
            var innerParam = innerLambda?.Parameters[0];
            var innerContext = context;

            if (innerParam is not null && context.ParameterName is not null) {
                var outerParam = FindOuterParameterExpression(expression.Arguments[0]);
                if (outerParam is not null) {
                    var outerParams = context.OuterParameters is not null
                        ? new Dictionary<ParameterExpression, TableName>(context.OuterParameters)
                        : new Dictionary<ParameterExpression, TableName>();
                    outerParams[outerParam] = context.ParameterName;

                    innerContext = new TranslationContext(
                        ParameterName: GetTableName(innerParam),
                        OuterParameters: outerParams
                    );
                }
            }

            var predicate = Translate(expression.Arguments[1], innerContext);

            if (predicate is Expr expr) {
                if (source is TableOrSubquery tableOrSubquery) {
                    var core = SelectCoreNormal.Create(tableOrSubquery, Where: expr);
                    return ApplyLimit(SelectStmt.Create(core), 1, expression);
                }
                if (source is SelectStmt selectStmt && selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal) {
                    var mergedWhere = selectCoreNormal.Where != null
                        ? ExprBinary.Create(BinaryOperator.AndAlso, selectCoreNormal.Where, expr)
                        : expr;
                    return ApplyLimit(selectStmt with {
                        SelectCores = StableList.Create<SelectCore>(selectCoreNormal with { Where = mergedWhere })
                    }, 1, expression);
                }
            }
        }
        throw new ExpressionNotSupportedException(expression);
    }

    protected virtual SelectStmt Single(MethodCallExpression expression, TranslationContext context) {
        // Single is like First but will throw at runtime if more than one row (SQL limitation: no runtime check)
        return First(expression, context);
    }

    private SelectStmt ApplyLimit(object source, int limit, MethodCallExpression expression) {
        SelectStmt selectStmt = source switch {
            SelectStmt s => s with { Limit = ExprBindConstant.Create(typeof(int), limit) },
            SelectCoreNormal core => SelectStmt.Create(core, Limit: ExprBindConstant.Create(typeof(int), limit)),
            TableOrSubquery table => SelectStmt.Create(SelectCoreNormal.Create(table), Limit: ExprBindConstant.Create(typeof(int), limit)),
            _ => throw new ExpressionNotSupportedException(expression)
        };
        return selectStmt;
    }

    protected virtual SelectStmt Compound(MethodCallExpression expression, TranslationContext context, CompoundOperator op) {
        if (expression.Arguments.Count == 2) {
            var source = Translate(expression.Arguments[0], context);
            var other = Translate(expression.Arguments[1], context);

            SelectStmt left = source switch {
                SelectStmt s => s,
                SelectCoreNormal core => SelectStmt.Create(core),
                TableOrSubquery table => SelectStmt.Create(SelectCoreNormal.Create(table)),
                _ => throw new ExpressionNotSupportedException(expression)
            };

            SelectStmt right = other switch {
                SelectStmt s => s,
                SelectCoreNormal core => SelectStmt.Create(core),
                TableOrSubquery table => SelectStmt.Create(SelectCoreNormal.Create(table)),
                _ => throw new ExpressionNotSupportedException(expression)
            };

            // Merge: take left's structure, add right's SelectCores
            var allCores = left.SelectCores.Concat(right.SelectCores).ToStableList();
            return left with {
                SelectCores = allCores,
                CompoundOperator = op,
            };
        }
        throw new ExpressionNotSupportedException(expression);
    }

    #region GroupBy

    /// <summary>
    /// Translates Queryable.GroupBy with various overloads.
    /// </summary>
    protected virtual SelectStmt GroupBy(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {
            // GroupBy(source, keySelector) → IQueryable<IGrouping<TKey, TSource>>
            var source = Translate(expression.Arguments[0], context);
            var keyLambda = UnwrapLambda(expression.Arguments[1]);

            if (keyLambda == null) {
                throw new ExpressionNotSupportedException("GroupBy key selector must be a lambda.", expression);
            }

            var keyExpr = TranslateGroupByKey(keyLambda, context);
            var groupBys = StableList.Create(keyExpr);

            return ApplyGroupBy(source, groupBys, expression);
        }

        if (expression.Arguments.Count == 3) {
            // Check if 3rd arg is element selector or result selector
            var thirdArg = expression.Arguments[2];
            if (IsLambdaReturningGrouping(thirdArg)) {
                // GroupBy(source, keySelector, elementSelector) → IQueryable<IGrouping<TKey, TElement>>
                var source = Translate(expression.Arguments[0], context);
                var keyLambda = UnwrapLambda(expression.Arguments[1]);
                if (keyLambda == null) {
                    throw new ExpressionNotSupportedException("GroupBy key selector must be a lambda.", expression);
                }
                var keyExpr = TranslateGroupByKey(keyLambda, context);
                var groupBys = StableList.Create(keyExpr);
                return ApplyGroupBy(source, groupBys, expression);
            }
            else {
                // GroupBy(source, keySelector, resultSelector) → IQueryable<TResult>
                var source = Translate(expression.Arguments[0], context);
                var keyLambda = UnwrapLambda(expression.Arguments[1]);
                var resultLambda = UnwrapLambda(thirdArg);

                if (keyLambda == null || resultLambda == null) {
                    throw new ExpressionNotSupportedException("GroupBy selectors must be lambdas.", expression);
                }

                var keyExpr = TranslateGroupByKey(keyLambda, context);
                var groupBys = StableList.Create(keyExpr);
                var groupedSource = ApplyGroupBy(source, groupBys, expression);

                // Translate the result selector in grouping context
                return TranslateGroupByResultSelector(groupedSource, resultLambda, groupBys, expression);
            }
        }

        if (expression.Arguments.Count == 4) {
            // GroupBy(source, keySelector, elementSelector, resultSelector) → IQueryable<TResult>
            var source = Translate(expression.Arguments[0], context);
            var keyLambda = UnwrapLambda(expression.Arguments[1]);
            // elementSelector is arguments[2] - we'll use it for future element filtering
            var resultLambda = UnwrapLambda(expression.Arguments[3]);

            if (keyLambda == null || resultLambda == null) {
                throw new ExpressionNotSupportedException("GroupBy selectors must be lambdas.", expression);
            }

            var keyExpr = TranslateGroupByKey(keyLambda, context);
            var groupBys = StableList.Create(keyExpr);
            var groupedSource = ApplyGroupBy(source, groupBys, expression);

            return TranslateGroupByResultSelector(groupedSource, resultLambda, groupBys, expression);
        }

        throw new ExpressionNotSupportedException(expression);
    }

    private static Expr TranslateGroupByKey(LambdaExpression keyLambda, TranslationContext context) {
        // The key lambda body could be a simple member access (u => u.IsActive)
        // or an anonymous type (u => new { u.Department, u.IsActive })
        var body = keyLambda.Body;

        if (body is MemberExpression member && member.Expression is ParameterExpression) {
            // Simple key: u => u.IsActive
            return ExprColumn.Create(
                TableName.Create(context.ParameterName?.Name ?? string.Empty, context.ParameterName?.Type ?? typeof(object)),
                ColumnName.Create(member.Member.Name, member.Member is PropertyInfo prop ? prop.PropertyType : typeof(object))
            );
        }

        // For complex keys, evaluate the expression to get the key value
        // This is a simplification - in production, you'd want to decompose anonymous types
        throw new ExpressionNotSupportedException("Complex group-by keys (anonymous types) are not yet supported.");
    }

    private SelectStmt ApplyGroupBy(object source, StableList<Expr> groupBys, MethodCallExpression expression) {
        return source switch {
            SelectCoreNormal core => SelectStmt.Create(core with { GroupBys = groupBys }),
            SelectStmt selectStmt when selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal =>
                selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with { GroupBys = groupBys })
                },
            TableOrSubquery table => SelectStmt.Create(SelectCoreNormal.Create(table) with { GroupBys = groupBys }),
            _ => throw new ExpressionNotSupportedException($"GroupBy source not supported: {source.GetType()}.", expression)
        };
    }

    private SelectStmt TranslateGroupByResultSelector(object source, LambdaExpression resultLambda, StableList<Expr> groupBys, MethodCallExpression expression) {
        // The result selector is (key, elements) => new { Key = key, Count = elements.Count() }
        // In expression tree form, the parameters represent the key and the IGrouping

        // For now, handle the common pattern: resultSelector body is a New expression
        // with g.Key and aggregate calls
        if (resultLambda.Body is NewExpression newExpr) {
            var resultColumns = new List<ResultColumn>();

            foreach (var arg in newExpr.Arguments) {
                var translated = TranslateGroupByExpression(arg, groupBys, expression);
                resultColumns.Add(ResultColumnExpr.Create(translated));
            }

            return CreateGroupedSelectStmt(source, resultColumns, groupBys, expression);
        }

        throw new ExpressionNotSupportedException("GroupBy result selector must be an anonymous type expression.", expression);
    }

    private Expr TranslateGroupByExpression(Expression expr, StableList<Expr> groupBys, MethodCallExpression expression) {
        // Handle g.Key → map to GROUP BY expression
        if (expr is MemberExpression member && member.Member.Name == "Key") {
            // g.Key maps to the first (and typically only) GROUP BY expression
            if (groupBys.Count == 1) {
                return groupBys[0];
            }
            // For multi-column keys, g.Key would be a tuple/anonymous type
            // For now, handle single-column case
            throw new ExpressionNotSupportedException("Multi-column group-by keys with g.Key are not yet supported.", expression);
        }

        // Handle g.Count() → COUNT(*)
        if (expr is MethodCallExpression methodCall) {
            if (methodCall.Method.Name == nameof(Enumerable.Count) && methodCall.Arguments.Count == 0) {
                return ExprFunction.Create(ExprFunctionName.Count, typeof(int));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Count) && methodCall.Arguments.Count == 1) {
                // g.Count() is Enumerable.Count(g) with 1 arg; Count(pred) is Enumerable.Count(g, pred) with 2 args
                return ExprFunction.Create(ExprFunctionName.Count, typeof(int));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Sum) && methodCall.Arguments.Count == 1) {
                // g.Sum(u => u.UserId) → Enumerable.Sum(g, u => u.UserId) with 2 args
                // Or g.Sum() with 1 arg (no selector)
                return ExprFunction.Create(ExprFunctionName.Sum, typeof(double));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Sum) && methodCall.Arguments.Count == 2) {
                var selector = UnwrapLambda(methodCall.Arguments[1]);
                if (selector != null && selector.Body is MemberExpression sumMember && sumMember.Expression is ParameterExpression) {
                    return ExprFunction.Create(ExprFunctionName.Sum,
                        ExprColumn.Create(ColumnName.Create(sumMember.Member.Name, sumMember.Member is PropertyInfo sumProp ? sumProp.PropertyType : typeof(object))));
                }
                return ExprFunction.Create(ExprFunctionName.Sum, typeof(double));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Average) && methodCall.Arguments.Count == 1) {
                return ExprFunction.Create(ExprFunctionName.Average, typeof(double));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Average) && methodCall.Arguments.Count == 2) {
                var selector = UnwrapLambda(methodCall.Arguments[1]);
                if (selector != null && selector.Body is MemberExpression avgMember && avgMember.Expression is ParameterExpression) {
                    return ExprFunction.Create(ExprFunctionName.Average,
                        ExprColumn.Create(ColumnName.Create(avgMember.Member.Name, avgMember.Member is PropertyInfo avgProp ? avgProp.PropertyType : typeof(object))));
                }
                return ExprFunction.Create(ExprFunctionName.Average, typeof(double));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Min) && methodCall.Arguments.Count == 1) {
                return ExprFunction.Create(ExprFunctionName.Min, typeof(object));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Min) && methodCall.Arguments.Count == 2) {
                var selector = UnwrapLambda(methodCall.Arguments[1]);
                if (selector != null && selector.Body is MemberExpression minMember && minMember.Expression is ParameterExpression) {
                    return ExprFunction.Create(ExprFunctionName.Min,
                        ExprColumn.Create(ColumnName.Create(minMember.Member.Name, minMember.Member is PropertyInfo minProp ? minProp.PropertyType : typeof(object))));
                }
                return ExprFunction.Create(ExprFunctionName.Min, typeof(object));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Max) && methodCall.Arguments.Count == 1) {
                return ExprFunction.Create(ExprFunctionName.Max, typeof(object));
            }
            if (methodCall.Method.Name == nameof(Enumerable.Max) && methodCall.Arguments.Count == 2) {
                var selector = UnwrapLambda(methodCall.Arguments[1]);
                if (selector != null && selector.Body is MemberExpression maxMember && maxMember.Expression is ParameterExpression) {
                    return ExprFunction.Create(ExprFunctionName.Max,
                        ExprColumn.Create(ColumnName.Create(maxMember.Member.Name, maxMember.Member is PropertyInfo maxProp ? maxProp.PropertyType : typeof(object))));
                }
                return ExprFunction.Create(ExprFunctionName.Max, typeof(object));
            }
        }

        throw new ExpressionNotSupportedException($"GroupBy expression not supported: {expr.GetType()}.", expression);
    }

    private SelectStmt CreateGroupedSelectStmt(object source, List<ResultColumn> resultColumns, StableList<Expr> groupBys, MethodCallExpression expression) {
        var cols = resultColumns.ToStableList();

        return source switch {
            SelectCoreNormal core => SelectStmt.Create(core with {
                ResultColumns = cols,
                GroupBys = groupBys,
            }),
            SelectStmt selectStmt when selectStmt.SelectCores.Count == 1 && selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal =>
                selectStmt with {
                    SelectCores = StableList.Create<SelectCore>(selectCoreNormal with {
                        ResultColumns = cols,
                        GroupBys = groupBys,
                    })
                },
            _ => throw new ExpressionNotSupportedException($"Grouped select source not supported: {source.GetType()}.", expression)
        };
    }

    private static bool IsLambdaReturningGrouping(Expression expr) {
        var lambda = UnwrapLambda(expr);
        if (lambda == null) return false;
        // Check if the return type implements IGrouping<,>
        var returnType = lambda.ReturnType;
        return returnType.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGrouping<,>));
    }

    private static SelectCoreNormal? GetGroupedCore(object source) {
        if (source is SelectCoreNormal core && core.GroupBys.Any()) {
            return core;
        }
        if (source is SelectStmt selectStmt &&
            selectStmt.SelectCores.Count == 1 &&
            selectStmt.SelectCores[0] is SelectCoreNormal sc &&
            sc.GroupBys.Any()) {
            return sc;
        }
        return null;
    }

    #endregion
}
