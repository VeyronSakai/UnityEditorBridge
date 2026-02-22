using System.Threading;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;

namespace UniCortex.Editor.Tests.UseCases
{
    [TestFixture]
    internal sealed class GetEditorStatusUseCaseTest
    {
        [Test]
        public void ExecuteAsync_WhenPlayingAndPaused_ReturnsCorrectStatus()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var editorApplication = new SpyEditorApplication { IsPlaying = true, IsPaused = true };
            var useCase = new GetEditorStatusUseCase(dispatcher, editorApplication);

            var result = useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.isPlaying);
            Assert.IsTrue(result.isPaused);
            Assert.AreEqual(1, dispatcher.CallCount);
        }

        [Test]
        public void ExecuteAsync_WhenNotPlayingAndNotPaused_ReturnsCorrectStatus()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var editorApplication = new SpyEditorApplication();
            var useCase = new GetEditorStatusUseCase(dispatcher, editorApplication);

            var result = useCase.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result.isPlaying);
            Assert.IsFalse(result.isPaused);
            Assert.AreEqual(1, dispatcher.CallCount);
        }
    }
}
