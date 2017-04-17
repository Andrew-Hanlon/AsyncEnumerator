using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AsyncEnumerator;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncEnumeratorTests
{
    [TestClass]
    public class TaskLikeObservableTests
    {
        [TestMethod]
        public async Task SequenceRunsCorrectly()
        {
            var sched = new TestScheduler();
            var results = new List<int>();

            Test1().SubscribeOn(sched).Subscribe(i => results.Add(i));

            sched.Start();

            CollectionAssert.AreEqual(results, new[] {1, 2, 3}, "Sequences do not match");
        }

        private static async TaskLikeObservable<int> Test1()
        {
            var ob = await TaskLikeObservable<int>.Capture();

            await ob.Subscription.ConfigureAwait(false);

            ob.OnNext(1);

            ob.OnNext(2);

            ob.OnNext(3);

            return ob.OnCompleted();
        }
    }
}
