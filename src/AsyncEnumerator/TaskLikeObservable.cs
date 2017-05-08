using System;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface ITaskLikeSubject<T>
    {
        Task Subscription { get; }
        T OnCompleted();
        void OnNext(T value);
    }

    [AsyncMethodBuilder(typeof(TaskLikeObservableMethodBuilder<>))]
    public class TaskLikeObservable<T> : TaskLikeBase, IObservable<T>, ITaskLikeSubject<T>, ITaskLike
    {
        private readonly Subject<T> _subject = new Subject<T>();

        private readonly TaskCompletionSource<bool> _subscribeTask = new TaskCompletionSource<bool>();

        public static TaskProvider<ITaskLikeSubject<T>> Capture() => TaskProvider<ITaskLikeSubject<T>>.Instance;

        public Task Subscription => _subscribeTask.Task;

        internal override void SetException(ExceptionDispatchInfo exception) => _subject.OnError(exception.SourceException);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var ret = _subject.Subscribe(observer);

            _subscribeTask.TrySetResult(true);

            return ret;
        }

        T ITaskLikeSubject<T>.OnCompleted()
        {
            _subject.OnCompleted();

            IsCompleted = true;

            return default(T);
        }

        void ITaskLikeSubject<T>.OnNext(T value) { _subject.OnNext(value); }
    }

    public struct TaskLikeObservableMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> _methodBuilder;

        public static TaskLikeObservableMethodBuilder<T> Create() => new TaskLikeObservableMethodBuilder<T>(new TaskLikeObservable<T>());

        internal TaskLikeObservableMethodBuilder(TaskLikeObservable<T> task)
        {
            _methodBuilder = AsyncTaskMethodBuilder<T>.Create();
            Task = task;
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

        public void SetException(Exception e) => Task.SetException(ExceptionDispatchInfo.Capture(e));

        public void SetResult(T value) => Task.SetCompletion();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if ((INotifyCompletion) awaiter is TaskProvider<ITaskLikeSubject<T>>.TaskProviderAwaiter provider)
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

        public TaskLikeObservable<T> Task { get; }
    }


}