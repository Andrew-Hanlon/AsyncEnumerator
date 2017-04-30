using System;
using System.Threading.Tasks;
using AsyncEnumerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncEnumeratorTests
{
    [TestClass]
    public class CoopTaskTests
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
            await seq.MoveNextAsync();            
            await seq.MoveNextAsync();
            
            Assert.IsFalse(await seq.MoveNextAsync(), $"Call to {nameof(seq.MoveNextAsync)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }

        [TestMethod]
        public async Task EnumerationAdvancesCorrectlyAndCompletes2()
        {
            var seq = Test2();

            await seq.MoveNextAsync();
            await seq.MoveNextAsync();
            await seq.MoveNextAsync();

            Assert.IsFalse(await seq.MoveNextAsync(), $"Call to {nameof(seq.MoveNextAsync)} did not return false after enumeration completed.");

            Assert.IsTrue(seq.IsCompleted, "Enumeration did not complete after return.");
        }


        private static async CoopTask ExceptionTest1()
        {
            var yield = await CoopTask.Capture();

            await yield.Yield();

            throw new Exception();
        }

        private static async CoopTask Test1()
        {
            var yield = await CoopTask.Capture();

            await yield.Yield();

            await yield.Yield();

            await yield.Yield();
        }

        private static async CoopTask Test2()
        {
            var yield = await CoopTask.Capture();

            for (var i = 1; i <= 3; i++)
            {
                await yield.Yield();
            }
        }

    }
}
