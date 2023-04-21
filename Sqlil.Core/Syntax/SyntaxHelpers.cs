using System.Text;

namespace Sqlil.Core.Syntax;

public static class SyntaxHelpers {

    public static string GenerateCommandText(object sqlSegments) {
        return GenerateCommandText(sqlSegments, 1, p => "@p" + p);
    }

    private static string GenerateCommandText(object sqlSegmentsOrSegment, int parameter, Func<int, string> parameterNamer) {

        if (sqlSegmentsOrSegment is string valueString) {
            return valueString;
        }

        else if (sqlSegmentsOrSegment is Identifier valueIdentifier) {
            return valueIdentifier.ToString();
        }

        else if (sqlSegmentsOrSegment is ExprBindParameter) {
            return parameterNamer(parameter);
        }

        else if (sqlSegmentsOrSegment is IEnumerable<object> items) {
            var sb = new StringBuilder();

            foreach (var item in items) {
                if (item is string text) {
                    sb.Append(text);
                }
                else if (item is Identifier identifier) {
                    sb.Append(identifier.ToString());
                }
                else if (item is ExprBindParameter) {
                    sb.Append(parameterNamer(parameter));
                    parameter++;
                }
                else if (item is IEnumerable<object> innerItems) {
                    var innerPrint = GenerateCommandText(innerItems, parameter, parameterNamer);
                    sb.Append(innerPrint);
                }
                else {
                    throw new NotSupportedException(sqlSegmentsOrSegment?.GetType().ToString());
                }
            }

            return sb.ToString();
        }

        else {
            throw new NotSupportedException(sqlSegmentsOrSegment?.GetType().ToString());
        }
    }

    internal static object[] Empty { get; } = Array.Empty<object>();

    internal static object[] Concat(IEnumerable<object> items) {
        return items.SelectMany(item => {
            if (item is string) {
                return new object[] { item };
            }
            else if (item is Identifier) {
                return new object[] { item };
            }
            else if (item is ExprBindParameter) {
                return new object[] { item };
            }
            else if (item is IEnumerable<object> innerItems) {
                return Concat(innerItems);
            }
            else {
                throw new NotSupportedException(item?.GetType().ToString());
            }
        }).ToArray();
    }

    internal static object[] Concat(object arg0, params object[] args) {
        return Concat(args.Prepend(arg0));
    }


    /// <summary>
    /// Yields items with the separator in between each one.
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="items"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static object[] Join(string separator, object item0, params object[] items) {
        return Join(separator, items: items.Prepend(item0));
    }

    /// <summary>
    /// Yields items with the separator in between each one.
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="items"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal static object[] Join(string separator, IEnumerable<object?> items) {
        IEnumerable<object> nonNull = items
            .Where(x => x != null && x != Empty)!;

        var result =
            nonNull.Take(1)
            .Concat(nonNull.Skip(1).SelectMany(x => new object[] { separator, Concat(x) }))
            .ToArray();

        return result;
    }
}
