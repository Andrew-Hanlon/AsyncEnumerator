using System;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
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

}