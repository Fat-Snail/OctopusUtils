namespace OctopusEx.WebCore.Attributes.HangfireJob;

public class SingleInstanceAttribute : JobFilterAttribute, IServerFilter
{
    private static readonly ConcurrentDictionary<String, Boolean> RunningJobs = new();

    public void OnPerforming(PerformingContext context)
    {
        var jobKey = $"{context.BackgroundJob.Job.Type.FullName}.{context.BackgroundJob.Job.Method.Name}";

        if ( !RunningJobs.TryAdd(jobKey, true) )
        {
            context.Canceled = true; // 如果已经在运行，取消当前执行
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobKey = $"{context.BackgroundJob.Job.Type.FullName}.{context.BackgroundJob.Job.Method.Name}";
        RunningJobs.TryRemove(jobKey, out _);
    }
}

// // 使用
// [SingleInstance]
// public void YourJobMethod()
// {
//     // 实现
// }
