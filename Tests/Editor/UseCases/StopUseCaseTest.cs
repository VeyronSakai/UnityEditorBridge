using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class StopUseCaseTest
    {
        [Test]
        public void ExecuteAsync_SetsIsPlayingFalse_And_DispatchesToMainThread()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var editorApplication = new SpyEditorApplication { IsPlaying = true };
            var useCase = new StopUseCase(dispatcher, editorApplication);

            useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(editorApplication.IsPlaying);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
