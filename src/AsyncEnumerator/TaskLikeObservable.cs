using System;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
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
    public class TaskLikeObservable<T> : IObservable<T>, ITaskLikeSubject<T>
    {
        private readonly Subject<T> _subject = new Subject<T>();

        private readonly TaskCompletionSource<bool> _subscribeTask = new TaskCompletionSource<bool>();

        public static TaskLikeObservableProvider Capture() => TaskLikeObservableProvider.Instance;

        public T Current { get; internal set; }

        public bool IsCompleted { get; internal set; }

        public Task Subscription => _subscribeTask.Task;

        public void SetException(Exception exception) => _subject.OnError(exception);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var ret = _subject.Subscribe(observer);

            _subscribeTask.TrySetResult(true);

            return ret;
        }

        internal AsyncEnumeratorAwaiter GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        T ITaskLikeSubject<T>.OnCompleted()
        {
            _subject.OnCompleted();

            IsCompleted = true;

            return default(T);
        }

        void ITaskLikeSubject<T>.OnNext(T value) { _subject.OnNext(value); }

        public class TaskLikeObservableProvider
        {
            public static readonly TaskLikeObservableProvider Instance = new TaskLikeObservableProvider();

            public AsyncEnumeratorProviderAwaiter GetAwaiter() => new AsyncEnumeratorProviderAwaiter();

            public class AsyncEnumeratorProviderAwaiter : INotifyCompletion
            {
                private TaskLikeObservable<T> _enumerator;

                public bool IsCompleted => _enumerator != null;

                public ITaskLikeSubject<T> GetResult() => _enumerator;

                public void OnCompleted(Action continuation, TaskLikeObservable<T> asyncEnumerator)
                {
                    _enumerator = asyncEnumerator;
                    Task.Run(continuation);
                }

                public void OnCompleted(Action continuation) => throw new InvalidOperationException("OnCompleted override with asyncEnumerator param must be called.");
            }
        }

        internal class AsyncEnumeratorAwaiter : INotifyCompletion
        {
            private readonly TaskLikeObservable<T> _task;
            private TaskAwaiter _taskAwaiter;

            internal AsyncEnumeratorAwaiter(TaskLikeObservable<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            internal bool IsCompleted => _task.IsCompleted;

            public void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }

            internal void GetResult() { }
        }
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

        public void SetException(Exception e) => Task.SetException(e);

        public void SetResult(T value) => Task.IsCompleted = true;

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var provider = awaiter as TaskLikeObservable<T>.TaskLikeObservableProvider.AsyncEnumeratorProviderAwaiter;

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

        public TaskLikeObservable<T> Task { get; }
    }
}