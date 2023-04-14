using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Sqlil.Core.ExpressionTranslation;

[Serializable]
public class ExpressionNotSupportedException : Exception {
    public ExpressionNotSupportedException(Expression expression)
    : this(expression, null) { }

    public ExpressionNotSupportedException(Expression expression, Exception? innerException)
    : base($"Not supported: {expression.NodeType} of {expression.GetType().Name} for {expression}", innerException) { }

    protected ExpressionNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
