namespace OctopusEx.WebCore.DomainCore.EFAdapters;

public static class EFQueryableExtensions
{

    /// <summary>
    /// WhereIf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">数据源</param>
    /// <param name="predicate">执行表达式</param>
    /// <param name="condition">执行条件(True则执行predicate)</param>
    /// <returns></returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, Boolean condition) => condition ? source.Where(predicate) : source;
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, Boolean condition, Expression<Func<T, Boolean>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }
    /// <summary>
    /// WhereIf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">数据源</param>
    /// <param name="predicate">执行表达式</param>
    /// <param name="condition">执行条件(True则执行predicate)</param>
    /// <returns></returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, Expression<Func<T, Int32, Boolean>> predicate, Boolean condition)
    {
        return condition ? source.Where(predicate) : source;
    }
    /// <summary>
    /// WhereIf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">数据源</param>
    /// <param name="predicate">执行表达式</param>
    /// <param name="condition">执行条件(True则执行predicate)</param>
    /// <returns></returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, Func<T, Boolean> predicate, Boolean condition)
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// WhereIf
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">数据源</param>
    /// <param name="predicate">执行表达式</param>
    /// <param name="condition">执行条件(True则执行predicate)</param>
    /// <returns></returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, Func<T, int, bool> predicate, Boolean condition)
    {
        return condition ? source.Where(predicate) : source;
    }

    private static String _GetExpressionPropertyName<T>(Expression<Func<T, object>> expr)
    {
        var rtn = "";
        if ( expr.Body is UnaryExpression )
        {
            rtn = (( MemberExpression )(( UnaryExpression )expr.Body).Operand).Member.Name;
        }
        else if ( expr.Body is MemberExpression )
        {
            rtn = (( MemberExpression )expr.Body).Member.Name;
        }
        else if ( expr.Body is ParameterExpression )
        {
            rtn = (( ParameterExpression )expr.Body).Type.Name;
        }
        return rtn;
    }

    ///// <summary>
    ///// 动态匹配范围
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="source"></param>
    ///// <param name="predicate"></param>
    ///// <param name="min">大于等于min</param>
    ///// <param name="max">小于max</param>
    ///// <param name="condition"></param>
    ///// <returns></returns>
    //public static IQueryable<T> WhereIfRange<T>(this IQueryable<T> source, Expression<Func<T, object>> predicate, int? min, int? max, Boolean condition)
    //{
    //    // 字段动态范围
    //    if (condition)
    //    {

    //        Type type = typeof(T);
    //        var parameter = Expression.Parameter(type, "m");


    //        PropertyInfo property = type.GetProperty(_GetExpressionPropertyName(predicate));
    //        Expression expProperty = Expression.Property(parameter, property.Name);


    //        if (min.HasValue)
    //        {
    //            Expression<Func<object>> valueLamda = () => min.Value;
    //            Expression expValue = Expression.Convert(valueLamda.Body, property.PropertyType);
    //            Expression expression = Expression.GreaterThanOrEqual(expProperty, expValue); // GreaterThan大于   GreaterThanOrEqual大于或等于
    //            Expression<Func<T, bool>> filter = ((Expression<Func<T, bool>>)Expression.Lambda(expression, parameter));
    //            source = source.Where(filter);
    //        }

    //        if (max.HasValue)
    //        {
    //            Expression<Func<object>> valueLamdaMax = () => max.Value;
    //            Expression expValueMax = Expression.Convert(valueLamdaMax.Body, property.PropertyType);
    //            Expression expressionMax = Expression.LessThan(expProperty, expValueMax); // LessThan 小于   LessThanOrEqual 小于或等于
    //            Expression<Func<T, bool>> filterMax = ((Expression<Func<T, bool>>)Expression.Lambda(expressionMax, parameter));
    //            source = source.Where(filterMax);
    //        }
    //    }

    //    return source;
    //}


    ///// <summary>
    ///// 动态匹配范围
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    ///// <param name="source"></param>
    ///// <param name="field"></param>
    ///// <param name="min">大于等于min</param>
    ///// <param name="max">小于max</param>
    ///// <param name="condition"></param>
    ///// <returns></returns>
    //public static IQueryable<T> WhereIfRange<T>(this IQueryable<T> source, String field, int? min, int? max, Boolean condition)
    //{
    //    // 字段动态范围
    //    if (condition)
    //    {
    //        //var attrs = field.Split(".").ToList();
    //        //PropertyInfo topProp = null;
    //        //ParameterExpression paramExp = null;
    //        //MemberExpression memberExp = null;
    //        //List<MemberExpression> exps = new List<MemberExpression>();
    //        //foreach (var attr in attrs)
    //        //{
    //        //    if (attrs.IndexOf(attr) == 0)
    //        //    {
    //        //        topProp = GetPropertyInfo(typeof(T), attr);
    //        //        paramExp = Expression.Parameter(typeof(T));
    //        //    }
    //        //    if (memberExp == null)
    //        //    {
    //        //        memberExp = Expression.PropertyOrField(paramExp, attr);
    //        //    }
    //        //    else
    //        //    {
    //        //        memberExp = Expression.PropertyOrField(memberExp, attr);
    //        //    }
    //        //}
    //        //var lambda = Expression.Lambda(memberExp, paramExp);




    //        Type type = typeof(T);
    //        var parameter = Expression.Parameter(type, "m");
    //        PropertyInfo property = type.GetProperty(field);
    //        Expression expProperty = Expression.Property(parameter, property.Name);

    //        if (min.HasValue)
    //        {
    //            Expression<Func<object>> valueLamda = () => min.Value;
    //            Expression expValue = Expression.Convert(valueLamda.Body, property.PropertyType);
    //            Expression expression = Expression.GreaterThanOrEqual(expProperty, expValue); // GreaterThan大于   GreaterThanOrEqual大于或等于
    //            Expression<Func<T, bool>> filter = ((Expression<Func<T, bool>>)Expression.Lambda(expression, parameter));
    //            return source.Where(filter);
    //        }

    //        if (max.HasValue)
    //        {
    //            Expression<Func<object>> valueLamdaMax = () => max.Value;
    //            Expression expValueMax = Expression.Convert(valueLamdaMax.Body, property.PropertyType);
    //            Expression expressionMax = Expression.LessThan(expProperty, expValueMax); // LessThan 小于   LessThanOrEqual 小于或等于
    //            Expression<Func<T, bool>> filterMax = ((Expression<Func<T, bool>>)Expression.Lambda(expressionMax, parameter));
    //            return source.Where(filterMax);
    //        }
    //    }

    //    return source;
    //}

    /// <summary>
    /// 获取反射
    /// </summary>
    /// <param name="objType"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private static PropertyInfo GetPropertyInfo(Type objType, String name)
    {
        var properties = objType.GetProperties();
        var matchedProperty = properties.FirstOrDefault(p => p.Name == name);
        if ( matchedProperty == null )
            throw new ArgumentException("name");

        return matchedProperty;
    }
}
