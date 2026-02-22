using UniCortex.Editor.Domains.Interfaces;

namespace UniCortex.Editor.Tests.TestDoubles
{
    internal sealed class SpyCompilationPipeline : ICompilationPipeline
    {
        public int RequestScriptCompilationCallCount { get; private set; }

        public void RequestScriptCompilation() => RequestScriptCompilationCallCount++;
    }
}
