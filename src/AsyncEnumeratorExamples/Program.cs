using System;
using System.Threading.Tasks;
using AsyncEnumerator;
using System.Reactive.Linq;

namespace AsyncEnumeratorExamples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("AsyncEnumerator:");
            Consumer().GetAwaiter().GetResult();

            Console.WriteLine("\nAsyncParallelEnumerator:");
            Consumer2().GetAwaiter().GetResult();

            Console.WriteLine("\nTaskLikeObservable:");
            Consumer3().GetAwaiter().GetResult();

            Console.ReadKey();
        }

        public static async AsyncEnumerator<int> Producer()
        {
            var e = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

            await e.YieldInit();                          // Optionally Wait for first MoveNext call

            await Task.Delay(100).ConfigureAwait(false);  // Use any async constructs

            await e.Yield(1);                             // Yield a value and wait for MoveNext

            await Task.Delay(100);

            await e.Yield(2);

            return e.YieldReturn();                       // Return false to awaiting MoveNext
        }

        public static async Task Consumer()
        {
            var p = Producer();

            while (await p.MoveNext())          // Await the next value 
            {
                Console.WriteLine(p.Current);   // Use the current value
            }
        }


        public static async Task Consumer2()
        {
            var p = Producer2();

            while (await p.MoveNext())
            {
                Console.WriteLine(p.Current);
            }
        }

        public static async AsyncParallelEnumerator<int> Producer2()
        {
            var e = await AsyncParallelEnumerator<int>.Capture(); // Capture the underlying 'Task'

            await Task.Delay(100).ConfigureAwait(false);          // Use any async constructs

            e.Yield(1);                                           // Yield the value and continue

            await Task.Delay(100).ConfigureAwait(false);

            e.Yield(2);

            await Task.Delay(100).ConfigureAwait(false);

            e.Yield(3);

            return e.YieldReturn();                               // Returns false to awaiting MoveNext
        }

        public static Task Consumer3()
        {
            return Producer3().ForEachAsync(s => Console.WriteLine(s));
        }

        public static async TaskLikeObservable<string> Producer3()
        {
            var e = await TaskLikeObservable<string>.Capture();

            await e.Subscription;

            for (var i = 0; i < 10; i++)
            {
                e.OnNext("y" + i);

                await Task.Delay(100).ConfigureAwait(false);
            }

            return e.OnCompleted();
        }
    }
}
