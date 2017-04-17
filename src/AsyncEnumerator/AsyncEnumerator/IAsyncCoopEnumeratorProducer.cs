using System.Threading.Tasks;

namespace AsyncEnumerator {
    public interface IAsyncCoopEnumeratorProducer<T> {
        Task Yield(T value);
        Task YieldInit();
        T YieldReturn();
    }

    public interface IAsyncEnumeratorProducer<T> {
        void Yield(T value);        
        T YieldReturn();
    }
}