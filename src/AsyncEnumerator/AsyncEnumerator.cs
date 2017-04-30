using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface IAsyncEnumeratorProducer<T>
    {
        T Break();
        Task Pause();
        Task Return(T value);
    }

    [AsyncMethodBuilder(typeof(AsyncEnumeratorMethodBuilder<>))]
    public class AsyncEnumerator<T> : IAsyncEnumeratorProducer<T>, IAsyncEnumerator<T>, ITaskLike
    {
        private ExceptionDispatchInfo _exception;

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;

        public static TaskProvider Capture() => TaskProvider.Instance;

        public T Current { get; internal set; }

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

            return _yieldSource is null ? Task.FromResult(true) : _nextSource.Task;
        }

        internal void SetCompletion() => IsCompleted = true;

        internal void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        T IAsyncEnumeratorProducer<T>.Break()
        {
            _nextSource.TrySetResult(false);
            return default(T);
        }

        Task IAsyncEnumeratorProducer<T>.Pause()
        {
            _isStarted = true;
            _yieldSource = new TaskCompletionSource<bool>();
            return _yieldSource.Task;
        }

        Task IAsyncEnumeratorProducer<T>.Return(T value)
        {
            Current = value;

            _yieldSource = new TaskCompletionSource<bool>();

            _nextSource?.TrySetResult(true);

            return _yieldSource.Task;
        }

        public class TaskProvider
        {
            public static readonly TaskProvider Instance = new TaskProvider();

            public TaskProviderAwaiter GetAwaiter() => new TaskProviderAwaiter();

            public class TaskProviderAwaiter : ITaskProviderAwaiter
            {
                private AsyncEnumerator<T> _enumerator;

                public bool IsCompleted => _enumerator != null;

                public IAsyncEnumeratorProducer<T> GetResult() => _enumerator;

                public void OnCompleted(Action continuation) => throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called.");

                public void OnCompleted(Action continuation, ITaskLike asyncEnumerator)
                {
                    _enumerator = (AsyncEnumerator<T>) asyncEnumerator;
                    continuation();
                }
            }
        }

        internal class AsyncEnumeratorAwaiter : TaskLikeAwaiterBase
        {
            private readonly AsyncEnumerator<T> _task;
            private TaskAwaiter _taskAwaiter;

            internal AsyncEnumeratorAwaiter(AsyncEnumerator<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void GetResult() => _task._exception?.Throw();

            public override void OnCompleted(Action a) => _taskAwaiter.OnCompleted(a);
        }
    }

    public class AsyncEnumeratorMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder _methodBuilder;

        public static AsyncEnumeratorMethodBuilder<T> Create() => new AsyncEnumeratorMethodBuilder<T>(new AsyncEnumerator<T>());

        internal AsyncEnumeratorMethodBuilder(AsyncEnumerator<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder.Create();
            Task = task;
        }

        public AsyncEnumerator<T> Task { get; }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if ((INotifyCompletion) awaiter is ITaskProviderAwaiter provider)
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

        public void SetException(Exception ex) => Task.SetException(ExceptionDispatchInfo.Capture(ex));

        public void SetResult(T result) => Task.SetCompletion();

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
    }
}