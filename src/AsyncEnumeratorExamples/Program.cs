using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using AsyncEnumerator;
using System.Reactive.Linq;

namespace AsyncEnumeratorExamples
{
    internal class Program
    {
        private static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
        {
            Console.WriteLine("AsyncEnumerator:");
            await Consumer();

            Console.WriteLine("\nAsyncSequence:");
            await Consumer2();

            Console.WriteLine("\nTaskLikeObservable:");
            await Consumer3();

            Console.WriteLine("\nCoopTask:");
            await Consumer4();

            Console.WriteLine("\nCoopTask2:");
            await Consumer5();

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

            return yield.Break();       // Return false to awaiting MoveNext
        }

        public static async Task Consumer()
        {
            var p = Producer();

            while (await p.MoveNextAsync())     // Await the next value
            {
                Console.WriteLine(p.Current);   // Use the current value
            }
        }


        public static async Task Consumer2()
        {
            var p = Producer2().Where(i => i % 2 == 0); 

            while (await p.MoveNextAsync())
            {
                Console.WriteLine(p.Current);
            }
        }

        public static async AsyncSequence<int> Producer2()
        {
            var seq = await AsyncSequence<int>.Capture(); // Capture the underlying 'Task'

            for (int i = 1; i <= 5; i++)
            {
                await Task.Delay(100).ConfigureAwait(false); // Use any async constructs

                seq.Return(i);  // Return to an awaiting MoveNext, or queue the result.
            }

            return seq.Break();                           // Returns false to awaiting MoveNext
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

        public static async CoopTask Producer4()
        {
            var task = await CoopTask.Capture();          // Capture the underlying 'Task'

            Console.WriteLine("P0");

            await task.Yield();                           // Yield control back to parent

            await Task.Delay(100).ConfigureAwait(false);  // Use any async constructs

            Console.WriteLine("P1");

            await task.Yield();                           // Yield control

            await Task.Delay(100);

            Console.WriteLine("P2");

            await task.Break();                           // Mark the task as completed

            Console.WriteLine("P3");                      // Will not be run.
        }

        public static async Task Consumer4()
        {
            var p = Producer4();

            var i = 1;

            while (await p.MoveNextAsync())          // Await the next child operation
            {
                Console.WriteLine("C" + i++);
            }
        }

        public static async CoopTask Producer5()
        {
            var task = await CoopTask.Capture();          // Capture the underlying 'Task'

            Console.WriteLine("P 0");

            await task.Yield();                           // Yield control back to parent

            await Task.Delay(100).ConfigureAwait(false);  // Use any async constructs

            Console.WriteLine("P 1");

            await task.Yield();                           // Yield control

            await Task.Delay(100);

            Console.WriteLine("P 2");

            await task.Break();                           // Mark the task as completed

            Console.WriteLine("P 3");                     // Will not be run.
        }

        public static async Task Consumer5()
        {
            Console.WriteLine("C 0");

            var p = Producer5();

            await p.MoveNextAsync();

            Console.WriteLine("C 1");

            await p.MoveNextAsync();

            Console.WriteLine("C 2");

            await p.MoveNextAsync();

            Console.WriteLine("C 3");

            await p;
        }
    }
}
