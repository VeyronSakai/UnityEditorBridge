using UniCortex.Editor.Domains.Interfaces;

namespace UniCortex.Editor.Tests.TestDoubles
{
    internal sealed class SpyEditorApplication : IEditorApplication
    {
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
    }
}
