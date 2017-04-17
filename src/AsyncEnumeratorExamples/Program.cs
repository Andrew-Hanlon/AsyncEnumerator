using System;
using System.Threading.Tasks;
using AsyncEnumerator;

namespace AsyncEnumeratorExamples
{
    class Program
    {
        static void Main(string[] args)
        {
            Consumer().GetAwaiter().GetResult();

            Console.ReadKey();
        }

        public static async Task Consumer()
        {
            var p = Producer();

            while (await p.MoveNext())
            {
                Console.WriteLine(p.Current);
            }

            Producer2().Subscribe(Console.WriteLine);
        }

        public static async AsyncEnumerator<string> Producer()
        {
            var e = await AsyncEnumerator<string>.Capture();

            for (var i = 0; i < 10; i++)
            {
                e.Yield("y" + i);

                await Task.Delay(100).ConfigureAwait(false);
            }

            return e.YieldReturn();
        }

        public static async TaskLikeObservable<string> Producer2()
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
