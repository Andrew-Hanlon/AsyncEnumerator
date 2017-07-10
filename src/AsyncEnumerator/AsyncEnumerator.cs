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
    public class AsyncEnumerator<T> : TaskLikeBase, IAsyncEnumeratorProducer<T>, IAsyncEnumerator<T>, IDisposable
    {
        private ExceptionDispatchInfo _exception;

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;

        public static TaskProvider<IAsyncEnumeratorProducer<T>> Capture() => TaskProvider<IAsyncEnumeratorProducer<T>>.Instance;

        public T Current { get; internal set; }

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

        internal override void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        T IAsyncEnumeratorProducer<T>.Break()
        {
            IsCompleted = true;
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

        public void Dispose()
        {

            if (_yieldSource == null || !_yieldSource.Task.IsCompleted)
            {
                _yieldSource?.TrySetException(new AbandonedAsyncEnumeratorException());
            }
        }
    }

    internal class AbandonedAsyncEnumeratorException : Exception { }

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
            if ((INotifyCompletion) awaiter is TaskProvider<IAsyncEnumeratorProducer<T>>.TaskProviderAwaiter provider)
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

        public void SetException(Exception ex) => Task.SetException(ExceptionDispatchInfo.Capture(ex));

        public void SetResult(T result) => Task.SetCompletion();

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
    }
}