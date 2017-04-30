namespace AsyncEnumerator
{
    public interface ITaskLike
    {
        bool IsCompleted { get; }

        TaskLikeAwaiterBase GetAwaiter();
    }
}