using Sqlil.Core.Syntax;
using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

internal class Call {
    internal static object Translate(MethodCallExpression expression, TranslationContext context) {

        if (expression.Method.DeclaringType == typeof(Queryable)) {
            object result = expression.Method.Name switch {
                nameof(Queryable.Select) => Select(expression, context),
                nameof(Queryable.Join) => Join(expression, context),
                nameof(Queryable.Where) => Where(expression, context),
                nameof(Queryable.OrderBy) => OrderBy(expression, context),
                nameof(Queryable.OrderByDescending) => OrderByDescending(expression, context),
                nameof(Queryable.Skip) => Skip(expression, context),
                nameof(Queryable.Take) => Take(expression, context),
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
            ExprBinary result = TranslateStringMethod(expression, context);
            return result;
        }

        else {
            throw new ExpressionNotSupportedException(expression);
        }
    }

    private static ExprBinary TranslateStringMethod(MethodCallExpression expression, TranslationContext context) {
        ExprBinary result = expression.Method.Name switch {

            nameof(string.Contains) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)AnExpression.Translate(expression.Object!, context),
                ExprBinary.Create(ExpressionType.Add, ExprLiteralString.Create("%"), ExprBinary.Create(ExpressionType.Add, (Expr)AnExpression.Translate(expression.Arguments[0], context), ExprLiteralString.Create("%")))
            ),

            nameof(string.StartsWith) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)AnExpression.Translate(expression.Object!, context),
                ExprBinary.Create(ExpressionType.Add, (Expr)AnExpression.Translate(expression.Arguments[0], context), ExprLiteralString.Create("%"))
            ),

            nameof(string.EndsWith) => ExprBinary.Create(BinaryOperator.Like,
                (Expr)AnExpression.Translate(expression.Object!, context),
                ExprBinary.Create(ExpressionType.Add, ExprLiteralString.Create("%"), (Expr)AnExpression.Translate(expression.Arguments[0], context))
            ),

            _ => throw new ExpressionNotSupportedException($"Method not supported {expression.Method}.", expression),
        };
        return result;
    }

    private static SelectCoreNormal Where(MethodCallExpression expression, TranslationContext context) {

        if (expression.Arguments.Count == 2) {

            // IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            var source = AnExpression.Translate(expression.Arguments[0], context);
            var predicate = AnExpression.Translate(expression.Arguments[1], context);

            if (source is TableOrSubquery tableOrSubquery &&
                predicate is Expr expr) {

                var result = SelectCoreNormal.Create(tableOrSubquery, Where: expr);
                return result;
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
    private static SelectStmt Select(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {

            // IQueryable<TResult> Select<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
            var currentContext = context with {
                ParameterName = AnExpression.GetParameterName(expression.Arguments[1]),
            };
            var source = AnExpression.Translate(expression.Arguments[0], currentContext);
            var selector = AnExpression.Translate(expression.Arguments[1], currentContext);

            if (source is SelectStmt selectStmt) {

                if (selectStmt.SelectCores.Count == 1 &&
                    selectStmt.SelectCores[0] is SelectCoreNormal selectCoreNormal &&
                    selectCoreNormal.ResultColumns.Count == 1 &&
                    selectCoreNormal.ResultColumns[0] is ResultColumnAsterisk) {

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

    private static SelectStmt Join(MethodCallExpression expression, TranslationContext context) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Supported: <see cref="Queryable.OrderBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private static SelectStmt OrderBy(MethodCallExpression expression, TranslationContext context) {

        if (expression.Arguments.Count == 2) {

            // IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            var source = AnExpression.Translate(expression.Arguments[0], context);
            var keySelector = AnExpression.Translate(expression.Arguments[1], context);

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
    private static SelectStmt OrderByDescending(MethodCallExpression expression, TranslationContext context) {
        var orderBy = OrderBy(expression, context);

        var result = orderBy with {
            OrderingTerms = orderBy.OrderingTerms.Select(term => term with { Desc = true }).ToStableList()
        };
        return result;
    }

    private static SelectStmt Skip(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {
            // IQueryable<TSource> Skip<TSource>(this IQueryable<TSource> source, int count);
            var source = AnExpression.Translate(expression.Arguments[0], context);
            var count = AnExpression.Translate(expression.Arguments[1], context);

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

    private static SelectStmt Take(MethodCallExpression expression, TranslationContext context) {
        if (expression.Arguments.Count == 2) {
            // IQueryable<TSource> Take<TSource>(this IQueryable<TSource> source, int count);
            var source = AnExpression.Translate(expression.Arguments[0], context);
            var count = AnExpression.Translate(expression.Arguments[1], context);

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

    private static object CreateTuple(MethodCallExpression expression, TranslationContext context) {
        throw new ExpressionNotSupportedException(expression);
    }
}
