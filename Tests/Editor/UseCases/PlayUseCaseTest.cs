using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class PlayUseCaseTest
    {
        [Test]
        public void ExecuteAsync_SetsIsPlayingTrue_And_DispatchesToMainThread()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var editorApplication = new SpyEditorApplication();
            var useCase = new PlayUseCase(dispatcher, editorApplication);

            useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(editorApplication.IsPlaying);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
