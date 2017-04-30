using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface ICoopTaskProducer
    {
        void Break();
        Task Yield();
    }

    [AsyncMethodBuilder(typeof(CoopTaskMethodBuilder))]
    public class CoopTask : ICoopTaskProducer, ITaskLike
    {
        private ExceptionDispatchInfo _exception;

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;

        public static TaskProvider Capture() => TaskProvider.Instance;

        public bool IsCompleted { get; internal set; }

        public TaskLikeAwaiterBase GetAwaiter()
        {
            _exception?.Throw();
            return new AsyncEnumeratorAwaiter(this);
        }

        public Task<bool> MoveNextAsync()
        {
            _exception?.Throw();

            if (!_isStarted)
            {
                _isStarted = true;
                return Task.FromResult(true);
            }

            _nextSource = new TaskCompletionSource<bool>();

            _yieldSource?.TrySetResult(true);

            return _yieldSource == null ? Task.FromResult(true) : _nextSource.Task;
        }

        internal void SetCompletion()
        {
            IsCompleted = true;
            _nextSource.TrySetResult(false);
        }

        internal void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        void ICoopTaskProducer.Break() => _nextSource.TrySetResult(false);

        Task ICoopTaskProducer.Yield()
        {
            _yieldSource = new TaskCompletionSource<bool>();

            _nextSource?.TrySetResult(true);

            return _yieldSource.Task;
        }

        public class TaskProvider
        {
            public static readonly TaskProvider Instance = new TaskProvider();

            public TaskProviderAwaiter GetAwaiter() { return new TaskProviderAwaiter(); }

            public class TaskProviderAwaiter : INotifyCompletion, ITaskProviderAwaiter
            {
                private CoopTask _enumerator;

                public bool IsCompleted => _enumerator != null;

                public ICoopTaskProducer GetResult() { return _enumerator; }

                public void OnCompleted(Action continuation)
                {
                    throw new InvalidOperationException(
                                                        "OnCompleted override with asyncEnumerator param must be called.");
                }

                public void OnCompleted(Action continuation, ITaskLike asyncEnumerator)
                {
                    _enumerator = (CoopTask) asyncEnumerator;
                    continuation();
                }
            }
        }

        internal class AsyncEnumeratorAwaiter : TaskLikeAwaiterBase
        {
            private readonly CoopTask _task;
            private TaskAwaiter _taskAwaiter;

            internal AsyncEnumeratorAwaiter(CoopTask task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void GetResult() => _task._exception?.Throw();

            public override void OnCompleted(Action a) => _taskAwaiter.OnCompleted(a);
        }
    }

    public struct CoopTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder _methodBuilder;

        public static CoopTaskMethodBuilder Create() => new CoopTaskMethodBuilder(new CoopTask());

        internal CoopTaskMethodBuilder(CoopTask task)
        {
            _methodBuilder = AsyncTaskMethodBuilder.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception ex) => Task.SetException(ExceptionDispatchInfo.Capture(ex));

        public void SetResult() => Task.SetCompletion();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if ((INotifyCompletion) awaiter is CoopTask.TaskProvider.TaskProviderAwaiter provider)
                provider.OnCompleted(((IAsyncStateMachine) stateMachine).MoveNext, Task);
            else
                _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
                                                                    ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public CoopTask Task { get; }
    }
}