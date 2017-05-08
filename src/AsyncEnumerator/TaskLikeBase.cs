using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncEnumerator
{
    public abstract class TaskLikeBase : ITaskLike
    {
        public bool IsCompleted { get; protected set; }

        public virtual TaskLikeAwaiterBase GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        internal virtual void SetCompletion() => IsCompleted = true;

        internal abstract void SetException(ExceptionDispatchInfo exception);

        public class AsyncEnumeratorAwaiter : TaskLikeAwaiterBase
        {
            private readonly ITaskLike _task;
            private TaskAwaiter _taskAwaiter;

            internal AsyncEnumeratorAwaiter(ITaskLike task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void GetResult() { }

            public override void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }
        }
    }
}