using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface ICoopTaskProducer
    {
        Task Break();
        Task Yield();
    }

    [AsyncMethodBuilder(typeof(CoopTaskMethodBuilder))]
    public class CoopTask : TaskLikeBase, ICoopTaskProducer, ITaskLike
    {
        private ExceptionDispatchInfo _exception;

        private bool _isStarted;
        private TaskCompletionSource<bool> _nextSource;
        private TaskCompletionSource<bool> _yieldSource;

        public static TaskProvider<ICoopTaskProducer> Capture() => TaskProvider<ICoopTaskProducer>.Instance;

        public override TaskLikeAwaiterBase GetAwaiter()
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

        internal override void SetCompletion()
        {
            IsCompleted = true;
            _nextSource.TrySetResult(false);
        }

        internal override void SetException(ExceptionDispatchInfo exception)
        {
            _exception = exception;
            _nextSource?.TrySetException(exception.SourceException);
        }

        Task ICoopTaskProducer.Break()
        {
            IsCompleted = true;
            _nextSource.TrySetResult(false);
            return new TaskCompletionSource<bool>().Task;
        }

        Task ICoopTaskProducer.Yield()
        {
            _yieldSource = new TaskCompletionSource<bool>();

            _nextSource?.TrySetResult(true);

            return _yieldSource.Task;
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
            if ((INotifyCompletion) awaiter is TaskProvider<ICoopTaskProducer>.TaskProviderAwaiter provider)
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