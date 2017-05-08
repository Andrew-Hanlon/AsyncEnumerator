using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface IAsyncSequenceProducer<T>
    {
        T Break();
        void Return(T value);
    }

    [AsyncMethodBuilder(typeof(AsyncSequenceMethodBuilder<>))]
    public class AsyncSequence<T> : TaskLikeBase, IAsyncSequenceProducer<T>, IAsyncEnumerator<T>
    {
        private readonly ConcurrentQueue<T> _valueQueue = new ConcurrentQueue<T>();
        private ExceptionDispatchInfo _exception;

        private TaskCompletionSource<bool> _nextSource;

        public static TaskProvider<IAsyncSequenceProducer<T>> Capture() => TaskProvider<IAsyncSequenceProducer<T>>.Instance;

        public T Current { get; private set; }

        public async Task<bool> MoveNextAsync()
        {
            _exception?.Throw();

            if (_valueQueue.TryDequeue(out var value))
            {
                Current = value;
                return true;
            }

            if (IsCompleted)
                return false;

            _nextSource = new TaskCompletionSource<bool>();

            await _nextSource.Task;

            if (!_valueQueue.TryDequeue(out value))
                return !IsCompleted;

            Current = value;
            return true;
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            _exception?.Throw();

            if (_valueQueue.TryDequeue(out var value))
            {
                Current = value;
                return true;
            }

            if (IsCompleted)
                return false;

            _nextSource = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(() => _nextSource.TrySetCanceled()))
            {
                await _nextSource.Task;
            }

            if (!_valueQueue.TryDequeue(out value))
                return !IsCompleted;

            Current = value;
            return true;
        }

        internal override void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        T IAsyncSequenceProducer<T>.Break()
        {
            IsCompleted = true;
            _nextSource?.TrySetResult(false);
            return default(T);
        }

        void IAsyncSequenceProducer<T>.Return(T value)
        {
            _valueQueue.Enqueue(value);
            _nextSource?.TrySetResult(true);
        }

        internal class AsyncSequenceAwaiter : TaskLikeAwaiterBase
        {
            private readonly AsyncSequence<T> _task;
            private TaskAwaiter _taskAwaiter;

            internal AsyncSequenceAwaiter(AsyncSequence<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            public override bool IsCompleted => _task.IsCompleted;

            public override void GetResult() => _task._exception?.Throw();

            public override void OnCompleted(Action a) => _taskAwaiter.OnCompleted(a);
        }
    }

    public struct AsyncSequenceMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _methodBuilder;

        public static AsyncSequenceMethodBuilder<T> Create() => new AsyncSequenceMethodBuilder<T>(new AsyncSequence<T>());

        internal AsyncSequenceMethodBuilder(AsyncSequence<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder<T>.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception ex) => Task.SetException(ExceptionDispatchInfo.Capture(ex));

        public void SetResult(T value) => Task.SetCompletion();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // The requirement for this cast is ridiculous.
            if ((INotifyCompletion) awaiter is TaskProvider<IAsyncSequenceProducer<T>>.TaskProviderAwaiter provider)
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

        public AsyncSequence<T> Task { get; }
    }
}