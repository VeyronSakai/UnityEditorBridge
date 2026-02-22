using System.Threading;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.Infrastructures;
using UniCortex.Editor.Tests.TestDoubles;
using UniCortex.Editor.UseCases;
using NUnit.Framework;
using UniCortex.Editor.Handlers.Editor;

namespace UniCortex.Editor.Tests.Presentations
{
    [TestFixture]
    internal sealed class PingHandlerTest
    {
        [Test]
        public void HandlePing_Returns200WithPongResponse()
        {
            var dispatcher = new FakeMainThreadDispatcher();
            var useCase = new PingUseCase(dispatcher);
            var handler = new PingHandler(useCase);

            var router = new RequestRouter();
            handler.Register(router);

            var context = new FakeRequestContext { HttpMethod = "GET", Path = ApiRoutes.Ping };

            router.HandleRequestAsync(context, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(200, context.ResponseStatusCode);
            StringAssert.Contains("pong", context.ResponseBody);
        }
    }
}
