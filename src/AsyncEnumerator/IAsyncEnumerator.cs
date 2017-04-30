using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumerator
{
    public interface IAsyncEnumerator<out T>
    {
        T Current { get; }
        Task<bool> MoveNextAsync();
    }
}