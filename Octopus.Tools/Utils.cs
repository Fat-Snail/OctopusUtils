using Octopus.Tools;

namespace Octopus;

/// <summary>
/// 实用工具类，提供重试机制等常用功能
/// </summary>
public static class Utils
{
    /// <summary>
    /// 执行带重试机制的方法（泛型版本）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的方法</param>
    /// <param name="maxRetryCount">最大重试次数，默认3次</param>
    /// <param name="sleepTime">重试间隔时间（毫秒），默认100毫秒</param>
    /// <param name="throwOnFailure">失败时是否抛出异常，默认false</param>
    /// <param name="onRetry">重试时的回调函数，接收当前重试次数和异常信息</param>
    /// <returns>方法执行结果</returns>
    public static T RetryMethod<T>(Func<T> func, Int32 maxRetryCount = 3, Int32 sleepTime = 100,
        Boolean throwOnFailure = false, Action<Int32, Exception> onRetry = null)
    {
        if ( func == null )
            throw new ArgumentNullException(nameof(func));

        if ( maxRetryCount < 0 )
            throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "重试次数不能为负数");

        Exception lastException = null;

        for ( var attempt = 0; attempt <= maxRetryCount; attempt++ )
        {
            try
            {
                return func();
            }
            catch ( Exception ex )
            {
                lastException = ex;

                if ( attempt < maxRetryCount )
                {
                    onRetry?.Invoke(attempt + 1, ex);

                    if ( sleepTime > 0 )
                        Thread.Sleep(sleepTime);
                }
            }
        }

        // 所有重试都失败
        ConsoleEx.Error($"方法执行失败，已重试{maxRetryCount}次");

        if ( lastException != null )
            ConsoleEx.Error(lastException);

        if ( throwOnFailure )
            throw lastException ?? new Exception("重试机制执行失败");

        return default!;
    }

    /// <summary>
    /// 执行带重试机制的方法（无返回值版本）
    /// </summary>
    /// <param name="action">要执行的方法</param>
    /// <param name="maxRetryCount">最大重试次数，默认3次</param>
    /// <param name="sleepTime">重试间隔时间（毫秒），默认100毫秒</param>
    /// <param name="onRetry">重试时的回调函数，接收当前重试次数和异常信息</param>
    public static void RetryMethod(Action action, Int32 maxRetryCount = 3, Int32 sleepTime = 100,
        Action<Int32, Exception> onRetry = null)
    {
        if ( action == null )
            throw new ArgumentNullException(nameof(action));

        RetryMethod(() =>
        {
            HashSet<string> hashSet = new HashSet<string>();
            action();
            return true; // 返回虚拟值
        }, maxRetryCount, sleepTime, false, onRetry);
    }

    /// <summary>
    /// 异步执行带重试机制的方法
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="asyncFunc">要执行的异步方法</param>
    /// <param name="maxRetryCount">最大重试次数，默认3次</param>
    /// <param name="sleepTime">重试间隔时间（毫秒），默认100毫秒</param>
    /// <param name="throwOnFailure">失败时是否抛出异常，默认false</param>
    /// <param name="onRetry">重试时的回调函数，接收当前重试次数和异常信息</param>
    /// <returns>异步方法执行结果</returns>
    public static async Task<T> RetryMethodAsync<T>(Func<Task<T>> asyncFunc, Int32 maxRetryCount = 3,
        Int32 sleepTime = 100, Boolean throwOnFailure = false, Action<Int32, Exception> onRetry = null)
    {
        if ( asyncFunc == null )
            throw new ArgumentNullException(nameof(asyncFunc));

        if ( maxRetryCount < 0 )
            throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "重试次数不能为负数");

        Exception lastException = null;

        for ( var attempt = 0; attempt <= maxRetryCount; attempt++ )
        {
            try
            {
                return await asyncFunc().ConfigureAwait(false);
            }
            catch ( Exception ex )
            {
                lastException = ex;

                if ( attempt < maxRetryCount )
                {
                    onRetry?.Invoke(attempt + 1, ex);

                    if ( sleepTime > 0 )
                        await Task.Delay(sleepTime).ConfigureAwait(false);
                }
            }
        }

        // 所有重试都失败
        ConsoleEx.Error($"异步方法执行失败，已重试{maxRetryCount}次");

        if ( lastException != null )
            ConsoleEx.Error(lastException);

        if ( throwOnFailure )
            throw lastException ?? new Exception("异步重试机制执行失败");

        return default!;
    }

    /// <summary>
    /// 异步执行带重试机制的方法（无返回值版本）
    /// </summary>
    /// <param name="asyncAction">要执行的异步方法</param>
    /// <param name="maxRetryCount">最大重试次数，默认3次</param>
    /// <param name="sleepTime">重试间隔时间（毫秒），默认100毫秒</param>
    /// <param name="onRetry">重试时的回调函数，接收当前重试次数和异常信息</param>
    public static async Task RetryMethodAsync(Func<Task> asyncAction, Int32 maxRetryCount = 3,
        Int32 sleepTime = 100, Action<Int32, Exception> onRetry = null)
    {
        if ( asyncAction == null )
            throw new ArgumentNullException(nameof(asyncAction));

        await RetryMethodAsync(async () =>
        {
            await asyncAction().ConfigureAwait(false);
            return true; // 返回虚拟值
        }, maxRetryCount, sleepTime, false, onRetry).ConfigureAwait(false);
    }
}
