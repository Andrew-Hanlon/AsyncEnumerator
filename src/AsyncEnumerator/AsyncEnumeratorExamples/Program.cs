using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
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
            //var p = Producer();

            //while (await p.MoveNext())
            //{
            //    Console.WriteLine(p.Current);
            //}

            var p2 = Producer2();

            p2.Subscribe(o =>
            {
                Console.WriteLine(o);
                //await Task.Delay(100);
            });
        }

        public static async AsyncEnumerator<string> Producer()
        {
            var e = await AsyncEnumerator<string>.Capture();

            for (int i = 0; i < 10; i++)
            {
                e.Yield("y" + i);

                await Task.Delay(100).ConfigureAwait(false);
            }

            return e.YieldReturn();
        }

        public static async TaskLikeObservable<string> Producer2()
        {
            var e = await TaskLikeObservable<string>.Capture();

            for (int i = 0; i < 10; i++)
            {
                e.OnNext("y" + i);

                await Task.Delay(100).ConfigureAwait(false);
            }

            return e.OnCompleted();
        }
    }
}
