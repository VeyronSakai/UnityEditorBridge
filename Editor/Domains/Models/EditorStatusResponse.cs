using System;

#nullable enable

namespace UniCortex.Editor.Domains.Models
{
    [Serializable]
    public class EditorStatusResponse
    {
        public bool isPlaying;
        public bool isPaused;

        public EditorStatusResponse(bool isPlaying, bool isPaused)
        {
            this.isPlaying = isPlaying;
            this.isPaused = isPaused;
        }
    }
}
