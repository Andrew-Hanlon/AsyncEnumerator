using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    [AsyncMethodBuilder(typeof(AsyncCoopEnumeratorMethodBuilder<>))]
    public class AsyncCoopEnumerator<T>
        : IAsyncCoopEnumeratorProducer<T>, IAsyncEnumerator<T>
    {
        #region Fields

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;

        #endregion

        public T Current { get; internal set; }

        public bool IsCompleted { get; internal set; }

        public T Result { get; internal set; }

        public static AsyncEnumeratorProvider Capture() => AsyncEnumeratorProvider.Instance;

        public Task<bool> MoveNext()
        {
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

        Task IAsyncCoopEnumeratorProducer<T>.Yield(T value)
        {
            Current = value;

            _yieldSource = new TaskCompletionSource<bool>();

            _nextSource?.TrySetResult(true);

            return _yieldSource.Task;
        }

        Task IAsyncCoopEnumeratorProducer<T>.YieldInit()
        {
            _isStarted = true;
            _yieldSource = new TaskCompletionSource<bool>();
            return _yieldSource.Task;
        }

        T IAsyncCoopEnumeratorProducer<T>.YieldReturn()
        {
            _nextSource.TrySetResult(false);
            return default(T);
        }

        internal AsyncEnumeratorAwaiter GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        internal class AsyncEnumeratorAwaiter : INotifyCompletion
        {
            #region Fields

            private readonly AsyncCoopEnumerator<T> _task;
            private TaskAwaiter _taskAwaiter;

            #endregion

            internal AsyncEnumeratorAwaiter(AsyncCoopEnumerator<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            internal bool IsCompleted => _task.IsCompleted;

            public void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }

            internal T GetResult() => _task.Result;
        }

        public class AsyncEnumeratorProvider
        {
            public static readonly AsyncEnumeratorProvider Instance = new AsyncEnumeratorProvider();

            public AsyncEnumeratorProviderAwaiter GetAwaiter() => new AsyncEnumeratorProviderAwaiter();

            public class AsyncEnumeratorProviderAwaiter : INotifyCompletion
            {
                #region Fields

                private AsyncCoopEnumerator<T> _enumerator;

                #endregion

                public bool IsCompleted => _enumerator != null;

                public IAsyncCoopEnumeratorProducer<T> GetResult() => _enumerator;

                public void OnCompleted(Action continuation, AsyncCoopEnumerator<T> asyncEnumerator)
                {
                    _enumerator = asyncEnumerator;
                    continuation();
                }

                public void OnCompleted(Action continuation) => throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called.");
            }
        }       
    }

    public static class AsyncEnumeratorExtensions
    {
        
        public static async Task ForeachAsync<T>(this IAsyncEnumerator<T> iter, Func<T, Task> action)
        {
            while (await iter.MoveNext())
            {
                await action(iter.Current);
            }
        }

        public static async Task ForeachAsync<T>(this IAsyncEnumerator<T> iter, Action<T> action)
        {
            while (await iter.MoveNext())
            {
                action(iter.Current);
            }
        }

    }

    public struct AsyncCoopEnumeratorMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _methodBuilder;

        public static AsyncCoopEnumeratorMethodBuilder<T> Create() => new AsyncCoopEnumeratorMethodBuilder<T>(new AsyncCoopEnumerator<T>());

        internal AsyncCoopEnumeratorMethodBuilder(AsyncCoopEnumerator<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder<T>.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception e) => Task.IsCompleted = true;

        public void SetResult(T value)
        {
            Task.Result = value;
            Task.IsCompleted = true;
            ((IAsyncCoopEnumeratorProducer<T>) Task).Yield(value);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var provider = awaiter as AsyncCoopEnumerator<T>.AsyncEnumeratorProvider.AsyncEnumeratorProviderAwaiter;

            if (provider is null)
                _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            else
                provider.OnCompleted(((IAsyncStateMachine) stateMachine).MoveNext, Task);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public AsyncCoopEnumerator<T> Task { get; }
    }

}