using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface IAsyncEnumeratorProducer<T>
    {
        Task Return(T value);
        Task Pause();
        T Break();
    }

    [AsyncMethodBuilder(typeof(AsyncEnumeratorMethodBuilder<>))]
    public class AsyncEnumerator<T> : IAsyncEnumeratorProducer<T>, IAsyncEnumerator<T>, ITaskLike
    {
        public T Current { get; internal set; }

        public Task<bool> MoveNext()
        {
            _exception?.Throw();

            if (!_isStarted)
            {
                _isStarted = true;
                return Task.FromResult(true);
            }

            _nextSource = new TaskCompletionSource<bool>();

            _yieldSource?.TrySetResult(true);

            if (_yieldSource == null)
                return Task.FromResult(true);

            return _nextSource.Task;
        }

        Task IAsyncEnumeratorProducer<T>.Return(T value)
        {
            Current = value;

            _yieldSource = new TaskCompletionSource<bool>();

            _nextSource?.TrySetResult(true);

            return _yieldSource.Task;
        }

        Task IAsyncEnumeratorProducer<T>.Pause()
        {
            _isStarted = true;
            _yieldSource = new TaskCompletionSource<bool>();
            return _yieldSource.Task;
        }

        T IAsyncEnumeratorProducer<T>.Break()
        {
            _nextSource.TrySetResult(false);
            return default(T);
        }

        public bool IsCompleted { get; internal set; }

        public TaskLikeAwaiterBase GetAwaiter()
        {
            _exception?.Throw();
            return new AsyncEnumeratorAwaiter(this);
        }

        public static TaskProvider Capture() => TaskProvider.Instance;

        public void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        internal void SetCompletion() => IsCompleted = true;

        internal class AsyncEnumeratorAwaiter : TaskLikeAwaiterBase
        {
            internal AsyncEnumeratorAwaiter(AsyncEnumerator<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void GetResult(){}

            public override void OnCompleted(Action a) => _taskAwaiter.OnCompleted(a);

            #region Fields

            private readonly AsyncEnumerator<T> _task;
            private TaskAwaiter _taskAwaiter;

            #endregion
        }

        public class TaskProvider
        {
            public static readonly TaskProvider Instance = new TaskProvider();

            public TaskProviderAwaiter GetAwaiter()
            {
                return new TaskProviderAwaiter();
            }

            public class TaskProviderAwaiter : INotifyCompletion
            {
                #region Fields

                private AsyncEnumerator<T> _enumerator;

                #endregion

                public bool IsCompleted => _enumerator != null;

                public void OnCompleted(Action continuation)
                {
                    throw new InvalidOperationException(
                        "OnCompleted override with asyncEnumerator param must be called.");
                }

                public IAsyncEnumeratorProducer<T> GetResult()
                {
                    return _enumerator;
                }

                public void OnCompleted(Action continuation, AsyncEnumerator<T> asyncEnumerator)
                {
                    _enumerator = asyncEnumerator;
                    continuation();
                }
            }
        }

        #region Fields

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;
        private ExceptionDispatchInfo _exception;

        #endregion
    }

    public struct AsyncEnumeratorMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _methodBuilder;

        public static AsyncEnumeratorMethodBuilder<T> Create() => new AsyncEnumeratorMethodBuilder<T>(new AsyncEnumerator<T>());

        internal AsyncEnumeratorMethodBuilder(AsyncEnumerator<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder<T>.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine){}

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception ex) => Task.SetException(ExceptionDispatchInfo.Capture(ex));

        public void SetResult(T value)
        {
            Task.SetCompletion();
            ((IAsyncEnumeratorProducer<T>) Task).Return(value);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if ((INotifyCompletion) awaiter is AsyncEnumerator<T>.TaskProvider.TaskProviderAwaiter provider)
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

        public AsyncEnumerator<T> Task { get; }
    }
}