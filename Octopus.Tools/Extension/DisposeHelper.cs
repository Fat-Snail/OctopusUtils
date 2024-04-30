using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>销毁助手。扩展方法专用</summary>
[EditorBrowsable (EditorBrowsableState.Advanced)]
public static class DisposeHelper
{
    /// <summary>尝试销毁对象，如果有<see cref="T:System.IDisposable" />则调用</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static object? TryDispose (this object? obj)
    {
        if (obj == null) {
            return obj;
        }
        IEnumerable enumerable = obj as IEnumerable;
        if (enumerable != null) {
            IList list = obj as IList;
            if (list == null) {
                list = new List<object> ();
                foreach (object item in enumerable) {
                    if (item is IDisposable) {
                        list.Add (item);
                    }
                }
            }
            foreach (object item2 in list) {
                IDisposable disposable = item2 as IDisposable;
                if (disposable != null) {
                    try {
                        disposable.Dispose ();
                    } catch {
                    }
                }
            }
        }
        IDisposable disposable2 = obj as IDisposable;
        if (disposable2 != null) {
            try {
                disposable2.Dispose ();
                return obj;
            } catch {
                return obj;
            }
        }
        return obj;
    }
}