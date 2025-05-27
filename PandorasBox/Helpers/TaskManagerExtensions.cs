using ECommons.Automation.NeoTaskManager;
using System;

namespace PandorasBox.Helpers;
public static class TaskManagerExtensions
{
    public static void EnqueueWithTimeout(this TaskManager tm, Action func, int timeoutMs, string taskName, bool abortOnTimeout = true)
        => tm.Enqueue(func, taskName, new() { TimeLimitMS = timeoutMs, AbortOnTimeout = true });
    public static void EnqueueWithTimeout(this TaskManager tm, Action func, int timeoutMs, bool abortOnTimeout = true)
        => tm.Enqueue(func, new() { TimeLimitMS = timeoutMs, AbortOnTimeout = true });
    public static void EnqueueWithTimeout(this TaskManager tm, Func<bool?> func, int timeoutMs, string taskName, bool abortOnTimeout = true)
        => tm.Enqueue(func, taskName, new() { TimeLimitMS = timeoutMs, AbortOnTimeout = abortOnTimeout });
    public static void EnqueueWithTimeout(this TaskManager tm, Func<bool?> func, int timeoutMs, bool abortOnTimeout = true)
        => tm.Enqueue(func, new() { TimeLimitMS = timeoutMs, AbortOnTimeout = abortOnTimeout });
}
