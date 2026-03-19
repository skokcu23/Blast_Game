using System.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// Bridges DOTween with async/await.
/// Allows GameOrchestrator to await DOTween animations
/// without changing its step-based dispatch architecture.
/// </summary>
public static class DOTweenHelper
{
    /// <summary>
    /// Convert a DOTween Tween into an awaitable Task.
    /// </summary>
    public static Task ToTask(this Tween tween)
    {
        if (tween == null || !tween.IsActive())
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        tween.OnComplete(() => tcs.TrySetResult(true));
        tween.OnKill(() => tcs.TrySetResult(true));

        return tcs.Task;
    }

    /// <summary>
    /// Convert a DOTween Sequence into an awaitable Task.
    /// </summary>
    public static Task ToTask(this Sequence sequence)
    {
        if (sequence == null || !sequence.IsActive())
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        sequence.OnComplete(() => tcs.TrySetResult(true));
        sequence.OnKill(() => tcs.TrySetResult(true));

        return tcs.Task;
    }
}
