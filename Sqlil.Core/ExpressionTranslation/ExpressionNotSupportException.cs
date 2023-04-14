using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Sqlil.Core.ExpressionTranslation;

[Serializable]
public class ExpressionNotSupportedException : Exception {

    public ExpressionNotSupportedException(string? message, Expression expression, Exception? innerException)
    : base($"Not supported: {expression.NodeType} of {expression.GetType().Name} for {expression}." + message != null ? " " + message : string.Empty, innerException) { }

    public ExpressionNotSupportedException(string message, Expression expression)
    : this(message, expression, null) { }

    public ExpressionNotSupportedException(Expression expression)
    : this(null, expression, null) { }

    protected ExpressionNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
