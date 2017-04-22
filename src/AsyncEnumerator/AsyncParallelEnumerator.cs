using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface IAsyncParallelEnumeratorProducer<T>
    {
        void Yield(T value);
        T YieldReturn();
    }

    [AsyncMethodBuilder(typeof(AsyncParallelEnumeratorMethodBuilder<>))]
    public class AsyncParallelEnumerator<T>
        : IAsyncParallelEnumeratorProducer<T>, IAsyncEnumerator<T>, ITaskLike
    {
        #region Fields

        private TaskCompletionSource<bool> _nextSource;

        private readonly ConcurrentQueue<T> _valueQueue = new ConcurrentQueue<T>();

        #endregion

        public T Current { get; internal set; }

        public bool IsCompleted { get; internal set; }

        public static TaskProvider Capture() => TaskProvider.Instance;

        public async Task<bool> MoveNext()
        {
            if (_valueQueue.TryDequeue(out var value))
            {
                Current = value;
                return true;
            }

            if (IsCompleted)
                return false;

            _nextSource = new TaskCompletionSource<bool>();

            await _nextSource.Task;

            if (_valueQueue.TryDequeue(out value))
            {
                Current = value;
                return true;
            }

            if (IsCompleted)
                return false;

            return true;
        }

        void IAsyncParallelEnumeratorProducer<T>.Yield(T value)
        {
            _valueQueue.Enqueue(value);
            _nextSource?.TrySetResult(true);
        }

        T IAsyncParallelEnumeratorProducer<T>.YieldReturn()
        {
            IsCompleted = true;
            _nextSource?.TrySetResult(false);
            return default(T);
        }

        public TaskLikeAwaiterBase GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        internal class AsyncEnumeratorAwaiter : TaskLikeAwaiterBase
        {
            #region Fields

            private readonly AsyncParallelEnumerator<T> _task;
            private TaskAwaiter _taskAwaiter;

            #endregion

            internal AsyncEnumeratorAwaiter(AsyncParallelEnumerator<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }
        }

        public class TaskProvider
        {
            public static readonly TaskProvider Instance = new TaskProvider();

            public TaskProviderAwaiter GetAwaiter() => new TaskProviderAwaiter();

            public class TaskProviderAwaiter : INotifyCompletion
            {
                #region Fields

                private AsyncParallelEnumerator<T> _enumerator;

                #endregion

                public bool IsCompleted => _enumerator != null;

                public IAsyncParallelEnumeratorProducer<T> GetResult() => _enumerator;

                public void OnCompleted(Action continuation, AsyncParallelEnumerator<T> asyncEnumerator)
                {
                    _enumerator = asyncEnumerator;
                    Task.Run(continuation);
                }

                public void OnCompleted(Action continuation) => throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called.");
            }
        }
    }

    public struct AsyncParallelEnumeratorMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _methodBuilder;

        public static AsyncParallelEnumeratorMethodBuilder<T> Create() => new AsyncParallelEnumeratorMethodBuilder<T>(new AsyncParallelEnumerator<T>());

        internal AsyncParallelEnumeratorMethodBuilder(AsyncParallelEnumerator<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder<T>.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception e) => Task.IsCompleted = true;

        public void SetResult(T value) => Task.IsCompleted = true;

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // The requirement for this cast is ridiculous. Pattern matching doesn't work with generics...
            if((INotifyCompletion)awaiter is AsyncParallelEnumerator<T>.TaskProvider.TaskProviderAwaiter provider) 
                provider.OnCompleted(((IAsyncStateMachine) stateMachine).MoveNext, Task);
            else
                _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);        
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public AsyncParallelEnumerator<T> Task { get; }
    }
}
