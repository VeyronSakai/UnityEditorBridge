using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class PauseUseCaseTest
    {
        [Test]
        public void ExecuteAsync_SetsIsPausedTrue_And_DispatchesToMainThread()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var editorApplication = new SpyEditorApplication();
            var useCase = new PauseUseCase(dispatcher, editorApplication);

            useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(editorApplication.IsPaused);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
