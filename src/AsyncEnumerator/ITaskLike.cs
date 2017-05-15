namespace AsyncEnumerator
{
    public interface ITaskLike
    {
        bool IsCompleted { get; }

        TaskLikeBase.TaskLikeAwaiter GetAwaiter();
    }
}