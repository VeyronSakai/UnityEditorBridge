using System;

#nullable enable

namespace UniCortex.Editor.Domains.Models
{
    [Serializable]
    public class EditorStatusResponse
    {
        public bool isPlaying;

        public EditorStatusResponse(bool isPlaying)
        {
            this.isPlaying = isPlaying;
        }
    }
}
