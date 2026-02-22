namespace UniCortex.Editor.Domains.Interfaces
{
    internal interface IEditorApplication
    {
        bool IsPlaying { get; set; }
        bool IsPaused { get; set; }
    }
}
