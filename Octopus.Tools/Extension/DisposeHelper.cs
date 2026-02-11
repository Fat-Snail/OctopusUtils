/// <summary>销毁助手。扩展方法专用</summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class DisposeHelper
{
    /// <summary>尝试销毁对象，如果有<see cref="T:System.IDisposable" />则调用</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static Object? TryDispose(this Object? obj)
    {
        if ( obj == null )
        {
            return obj;
        }
        var enumerable = obj as IEnumerable;
        if ( enumerable != null )
        {
            var list = obj as IList;
            if ( list == null )
            {
                list = new List<Object>();
                foreach ( var item in enumerable )
                {
                    if ( item is IDisposable )
                    {
                        list.Add(item);
                    }
                }
            }
            foreach ( var item2 in list )
            {
                var disposable = item2 as IDisposable;
                if ( disposable != null )
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }
        var disposable2 = obj as IDisposable;
        if ( disposable2 != null )
        {
            try
            {
                disposable2.Dispose();
                return obj;
            }
            catch
            {
                return obj;
            }
        }
        return obj;
    }
}
