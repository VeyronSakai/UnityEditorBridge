using System;

#nullable enable

namespace UniCortex.Editor.Domains.Models
{
    [Serializable]
    public class ResumeResponse
    {
        public bool success;

        public ResumeResponse(bool success)
        {
            this.success = success;
        }
    }
}
