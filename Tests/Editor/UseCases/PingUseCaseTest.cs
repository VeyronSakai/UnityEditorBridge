using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class PingUseCaseTest
    {
        [Test]
        public void ExecuteAsync_ReturnsMessage_And_DispatchesToMainThread()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var useCase = new PingUseCase(dispatcher);

            var result = useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual("pong", result);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
