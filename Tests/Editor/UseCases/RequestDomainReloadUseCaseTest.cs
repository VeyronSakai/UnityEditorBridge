using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class RequestDomainReloadUseCaseTest
    {
        [Test]
        public void ExecuteAsync_CallsRequestScriptCompilation_And_DispatchesToMainThread()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var compilationPipeline = new SpyCompilationPipeline();
            var useCase = new RequestDomainReloadUseCase(dispatcher, compilationPipeline);

            useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, compilationPipeline.RequestScriptCompilationCallCount);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
