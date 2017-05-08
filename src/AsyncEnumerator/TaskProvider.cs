using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace AsyncEnumerator
{
    public class TaskProvider<TOutput>
    {
        internal static readonly TaskProvider<TOutput> Instance = new TaskProvider<TOutput>();

        public TaskProviderAwaiter GetAwaiter() { return new TaskProviderAwaiter(); }

        public class TaskProviderAwaiter : INotifyCompletion
        {
            private TOutput _task;

            public bool IsCompleted => _task != null;

            public TOutput GetResult() { return _task; }

            public void OnCompleted(Action continuation) { throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called."); }

            public void OnCompleted<TInput>(Action continuation, TInput task) where TInput : TOutput
            {
                _task = task;
                continuation();
            }
        }
    }
}