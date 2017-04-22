using System;
using System.Runtime.CompilerServices;

namespace AsyncEnumerator
{
    /// <summary>
    /// Base class for Task-like awaiters
    /// </summary>
    public abstract class TaskLikeAwaiterBase : INotifyCompletion
    {
        public abstract void OnCompleted(Action continuation);

        public virtual bool IsCompleted { get; protected set; }

        public virtual void GetResult() { }
    }
}