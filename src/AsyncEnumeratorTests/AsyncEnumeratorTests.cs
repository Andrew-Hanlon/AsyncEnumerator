﻿using System;
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
            while(await seq.MoveNext()){}
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes1()
        {
            var seq = Test1();

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 1, $"First call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 2, $"Call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 3, $"Call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            Assert.IsFalse(await seq.MoveNext(), $"Call to {nameof(seq.MoveNext)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes2()
        {
            var seq = Test2();

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 1, $"First call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 2, $"Call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            await seq.MoveNext();
            Assert.AreEqual(seq.Current, 3, $"Call to {nameof(seq.MoveNext)} did not advance the enumeration correctly.");

            Assert.IsFalse(await seq.MoveNext(), $"Call to {nameof(seq.MoveNext)} did not return false after enumeration completed.");

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

    }
}
