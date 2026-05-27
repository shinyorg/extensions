using System.Linq.Expressions;

namespace Shiny.Extensions.Reflector;


public static class ReflectorExpressionExtensions
{
    /// <summary>
    /// Extracts the property name from a member access expression (e.g. x => x.MyProp).
    /// Supports unary conversions for value type / nullable boxing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static string GetPropertyName<T, TRet>(this Expression<Func<T, TRet>> expression)
    {
        var body = expression.Body;

        if (body is UnaryExpression unary)
            body = unary.Operand;

        if (body is MemberExpression member)
            return member.Member.Name;

        throw new ArgumentException("Expression must be a single property access (e.g. x => x.Property)", nameof(expression));
    }


    /// <summary>
    /// Resolves property info on the reflector for the property identified by the expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    /// <param name="this"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static PropertyGeneratedInfo? GetProperty<T, TRet>(this IReflectorClass @this, Expression<Func<T, TRet>> expression)
        => @this.TryGetPropertyInfo(expression.GetPropertyName());


    /// <summary>
    /// Gets the value of a property identified by a lambda expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    /// <param name="this"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static TRet? GetValue<T, TRet>(this IReflectorClass @this, Expression<Func<T, TRet>> expression)
        => @this.GetValue<TRet>(expression.GetPropertyName());


    /// <summary>
    /// Sets the value of a property identified by a lambda expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    /// <param name="this"></param>
    /// <param name="expression"></param>
    /// <param name="value"></param>
    public static void SetValue<T, TRet>(this IReflectorClass @this, Expression<Func<T, TRet>> expression, TRet? value)
        => @this.SetValue(expression.GetPropertyName(), value);
}
