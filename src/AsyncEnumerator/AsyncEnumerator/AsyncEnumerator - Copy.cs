using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    [AsyncMethodBuilder(typeof(TaskLikeObservableMethodBuilder<>))]
    public class TaskLikeObservable<T>
        : IObservable<T>, ITaskLikeSubject<T>
    {
        #region Fields

        private readonly AsyncSubject<T> _subject = new AsyncSubject<T>();

        #endregion

        public T Current { get; internal set; }

        public bool IsCompleted { get; internal set; }

        public T Result { get; internal set; }

        public static TaskLikeObservableProvider Capture() => TaskLikeObservableProvider.Instance;

        internal AsyncEnumeratorAwaiter GetAwaiter() => new AsyncEnumeratorAwaiter(this);

        internal class AsyncEnumeratorAwaiter : INotifyCompletion
        {
            #region Fields

            private readonly TaskLikeObservable<T> _task;
            private TaskAwaiter _taskAwaiter;

            #endregion

            internal AsyncEnumeratorAwaiter(TaskLikeObservable<T> task)
            {
                _task = task;
                _taskAwaiter = new TaskAwaiter();
            }

            internal bool IsCompleted => _task.IsCompleted;

            public void OnCompleted(Action a) { _taskAwaiter.OnCompleted(a); }

            internal T GetResult() => _task.Result;
        }

        public class TaskLikeObservableProvider
        {
            public static readonly TaskLikeObservableProvider Instance = new TaskLikeObservableProvider();

            public AsyncEnumeratorProviderAwaiter GetAwaiter() => new AsyncEnumeratorProviderAwaiter();

            public class AsyncEnumeratorProviderAwaiter : INotifyCompletion
            {
                #region Fields

                private TaskLikeObservable<T> _enumerator;

                #endregion

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

        public IDisposable Subscribe(IObserver<T> observer) => _subject.Subscribe(observer);

        void ITaskLikeSubject<T>.OnNext(T value)
        {
            _subject.OnNext(value);
        }

        T ITaskLikeSubject<T>.OnCompleted()
        {
            _subject.OnCompleted();

            IsCompleted = true;

            return default(T);
        }
    }

    public interface ITaskLikeSubject<T>
    {
        void OnNext(T value);

        T OnCompleted();
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
