using System;

#nullable enable

namespace UniCortex.Editor.Domains.Models
{
    [Serializable]
    public class PlayStopResponse
    {
        public bool success;

        public PlayStopResponse(bool success)
        {
            this.success = success;
        }
    }
}
