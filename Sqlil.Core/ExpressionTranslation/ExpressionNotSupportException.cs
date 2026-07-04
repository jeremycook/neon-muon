using System.Linq.Expressions;

namespace Sqlil.Core.ExpressionTranslation;

public class ExpressionNotSupportedException : Exception {

    public ExpressionNotSupportedException(string? message, Expression expression, Exception? innerException)
    : base($"Not supported: {expression.NodeType} of {expression.GetType().Name} for {expression}." + message != null ? " " + message : string.Empty, innerException) { }

    public ExpressionNotSupportedException(string message, Expression expression)
    : this(message, expression, null) { }

    public ExpressionNotSupportedException(Expression expression)
    : this(null, expression, null) { }

    public ExpressionNotSupportedException(string message)
    : base(message) { }
}
