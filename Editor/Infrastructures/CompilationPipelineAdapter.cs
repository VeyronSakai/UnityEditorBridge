using UniCortex.Editor.Domains.Interfaces;
using UnityEditor.Compilation;

namespace UniCortex.Editor.Infrastructures
{
    internal sealed class CompilationPipelineAdapter : ICompilationPipeline
    {
        public void RequestScriptCompilation()
        {
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
