using System.Linq.Expressions;

namespace Octopus.SearchCore.Ext;

/// <summary>
/// linq扩展类
/// </summary>
public static class LinqExtension
{
    /// <summary>
    /// 与连接
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="left">左条件</param>
    /// <param name="right">右条件</param>
    /// <returns>新表达式</returns>
    internal static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        return CombineLambdas(left, right, ExpressionType.AndAlso);
    }

    private static Expression<Func<T, bool>> CombineLambdas<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, ExpressionType expressionType)
    {
        if (IsExpressionBodyConstant(left))
        {
            return right;
        }

        var visitor = new SubstituteParameterVisitor
        {
            Sub =
            {
                [right.Parameters[0]] = left.Parameters[0]
            }
        };

        Expression body = Expression.MakeBinary(expressionType, left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, left.Parameters[0]);
    }

    private static bool IsExpressionBodyConstant<T>(Expression<Func<T, bool>> left)
    {
        return left.Body.NodeType == ExpressionType.Constant;
    }

    internal class SubstituteParameterVisitor : ExpressionVisitor
    {
        public Dictionary<Expression, Expression> Sub = new Dictionary<Expression, Expression>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Sub.TryGetValue(node, out var newValue) ? newValue : node;
        }
    }
}