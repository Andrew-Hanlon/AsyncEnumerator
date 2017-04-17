using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    [AsyncMethodBuilder(typeof(AsyncEnumeratorMethodBuilder<>))]
    public class AsyncEnumerator<T>
        : IAsyncEnumeratorProducer<T>, IAsyncEnumerator<T>
    {
        #region Fields

        private TaskCompletionSource<bool> _nextSource;

        private readonly ConcurrentQueue<T> _valueQueue = new ConcurrentQueue<T>();

        #endregion

        public T Current { get; internal set; }

        public bool IsCompleted { get; internal set; }

        public T Result { get; internal set; }

        public static AsyncEnumeratorProvider Capture() => AsyncEnumeratorProvider.Instance;

        private readonly object _lock = new object();

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

        void IAsyncEnumeratorProducer<T>.Yield(T value)
        {
            _valueQueue.Enqueue(value);
            _nextSource?.TrySetResult(true);
        }

        T IAsyncEnumeratorProducer<T>.YieldReturn()
        {
            IsCompleted = true;
            _nextSource?.TrySetResult(false);
            return default(T);
        }

        internal AsyncEnumeratorAwaiter GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        internal class AsyncEnumeratorAwaiter : INotifyCompletion
        {
            #region Fields

            private readonly AsyncEnumerator<T> _task;
            private TaskAwaiter _taskAwaiter;

            #endregion

            internal AsyncEnumeratorAwaiter(AsyncEnumerator<T> task)
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

                private AsyncEnumerator<T> _enumerator;

                #endregion

                public bool IsCompleted => _enumerator != null;

                public IAsyncEnumeratorProducer<T> GetResult() => _enumerator;

                public void OnCompleted(Action continuation, AsyncEnumerator<T> asyncEnumerator)
                {
                    _enumerator = asyncEnumerator;
                    Task.Run(continuation);
                }

                public void OnCompleted(Action continuation) => throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called.");
            }
        }
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

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception e) => Task.IsCompleted = true;

        public void SetResult(T value)
        {
            Task.Result = value;
            Task.IsCompleted = true;
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var provider = awaiter as AsyncEnumerator<T>.AsyncEnumeratorProvider.AsyncEnumeratorProviderAwaiter;

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

        public AsyncEnumerator<T> Task { get; }
    }
}
