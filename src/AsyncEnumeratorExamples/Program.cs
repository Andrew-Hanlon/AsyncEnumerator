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
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            Console.WriteLine("AsyncEnumerator:");
            await Consumer();

            Console.WriteLine("\nAsyncParallelEnumerator:");
            await Consumer2();

            Console.WriteLine("\nTaskLikeObservable:");
            await Consumer3();

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey(true);
        }

        public static async AsyncEnumerator<int> Producer()
        {
            var yield = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

            await yield.Pause();                          // Optionally Wait for first MoveNext call

            await Task.Delay(100).ConfigureAwait(false);  // Use any async constructs

            await yield.Return(1);                        // Yield a value and wait for MoveNext

            await Task.Delay(100);

            await yield.Return(2);

            return yield.Break();                       // Return false to awaiting MoveNext
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

        public static async AsyncSequence<int> Producer2()
        {
            var yield = await AsyncSequence<int>.Capture(); // Capture the underlying 'Task'

            await Task.Delay(100).ConfigureAwait(false);          // Use any async constructs

            yield.Return(1);                                      // Yield the value and continue

            await Task.Delay(100).ConfigureAwait(false);

            yield.Return(2);

            await Task.Delay(100).ConfigureAwait(false);

            yield.Return(3);

            return yield.Break();                               // Returns false to awaiting MoveNext
        }

        public static Task Consumer3()
        {
            return Producer3().ForEachAsync(s => Console.WriteLine(s));
        }

        public static async TaskLikeObservable<string> Producer3()
        {
            var o = await TaskLikeObservable<string>.Capture();

            await o.Subscription;

            for (var i = 0; i < 10; i++)
            {
                o.OnNext("y" + i);

                await Task.Delay(100).ConfigureAwait(false);
            }

            return o.OnCompleted();
        }
    }
}
