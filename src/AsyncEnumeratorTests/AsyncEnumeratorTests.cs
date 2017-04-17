using System;
using System.Threading.Tasks;
using AsyncEnumerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncEnumeratorTests
{
    [TestClass]
    public class AsyncEnumeratorTests
    {
        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes1()
        {
            var iter = Test1();

            await iter.MoveNext();
            Assert.AreEqual(1, iter.Current, $"First call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            await iter.MoveNext();
            Assert.AreEqual(2, iter.Current, $"Call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            await iter.MoveNext();
            Assert.AreEqual(3, iter.Current, $"Call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            Assert.IsFalse(await iter.MoveNext(), $"Call to {nameof(iter.MoveNext)} did not return false after enumeration completed.");

            Assert.IsTrue(iter.IsCompleted, "Enumeration did not complete after return.");
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes2()
        {
            var iter = Test2();

            await iter.MoveNext();
            Assert.AreEqual(1, iter.Current, $"First call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            await iter.MoveNext();
            Assert.AreEqual(2, iter.Current, $"Call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            await iter.MoveNext();
            Assert.AreEqual(3, iter.Current, $"Call to {nameof(iter.MoveNext)} did not advance the enumeration correctly.");

            Assert.IsFalse(await iter.MoveNext(), $"Call to {nameof(iter.MoveNext)} did not return false after enumeration completed.");

            Assert.IsTrue(iter.IsCompleted, "Enumeration did not complete after return.");
        }

        private static async AsyncEnumerator<int> Test1()
        {
            var iter = await AsyncEnumerator<int>.Capture();

            await Task.Delay(0).ConfigureAwait(false);

            iter.Yield(1);

            iter.Yield(2);

            iter.Yield(3);

            return iter.YieldReturn();
        }

        private static async AsyncEnumerator<int> Test2()
        {
            var iter = await AsyncEnumerator<int>.Capture();

            await Task.Delay(0).ConfigureAwait(false);

            for (var i = 1; i <= 3; i++)
            {
                iter.Yield(i);
            }

            return iter.YieldReturn();
        }
    }
}
