using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncEnumerator
{
    public abstract class TaskLikeBase : ITaskLike
    {
        public bool IsCompleted { get; protected set; }

        public virtual TaskLikeAwaiter GetAwaiter() => new TaskLikeAwaiter(this);

        internal virtual void SetCompletion() => IsCompleted = true;

        internal abstract void SetException(ExceptionDispatchInfo exception);

        public class TaskLikeAwaiter : INotifyCompletion
        {
            private readonly ITaskLike _task;
            private TaskAwaiter _taskAwaiter;

            internal TaskLikeAwaiter(ITaskLike task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public bool IsCompleted => _task.IsCompleted;

            public void GetResult() { }

            public void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }
        }
    }
}