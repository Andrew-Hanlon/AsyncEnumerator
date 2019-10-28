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
        [ExpectedException(typeof(Exception), "Awaiting failed Task did not throw.")]
        public async Task ThrowsOnMoveNext()
        {            
            var seq = ExceptionTest1();
            while(await seq.MoveNextAsync()){}
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes1()
        {
            var seq = Test1();

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 1, $"First call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 2, $"Call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 3, $"Call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            Assert.IsFalse(await seq.MoveNextAsync(), $"Call to {nameof(seq.MoveNextAsync)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes2()
        {
            var seq = Test2();

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 1, $"First call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 2, $"Call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            await seq.MoveNextAsync();
            Assert.AreEqual(seq.Current, 3, $"Call to {nameof(seq.MoveNextAsync)} did not advance the enumeration correctly.");

            Assert.IsFalse(await seq.MoveNextAsync(), $"Call to {nameof(seq.MoveNextAsync)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }

        [TestMethod]
        public async Task EmptyEnumeratorTest()
        {
            var seq = GetEmptyEnumerator();

            Assert.IsFalse(await seq.MoveNextAsync(), $"Call to {nameof(seq.MoveNextAsync)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }

        private static async AsyncEnumerator<int> ExceptionTest1()
        {
            var yield = await AsyncEnumerator<int>.Capture();

            await yield.Return(1);

            throw new Exception();
        }

        private static async AsyncEnumerator<int> Test1()
        {
            var yield = await AsyncEnumerator<int>.Capture();

            await yield.Return(1);

            await yield.Return(2);

            await yield.Return(3);

            return yield.Break();
        }

        private static async AsyncEnumerator<int> Test2()
        {
            var yield = await AsyncEnumerator<int>.Capture();

            for (var i = 1; i <= 3; i++)
            {
                await yield.Return(i);
            }

            return yield.Break();
        }

        private static async AsyncEnumerator<int> GetEmptyEnumerator()
        {
            var yield = await AsyncEnumerator<int>.Capture();

            return yield.Break();
        }

    }
}
