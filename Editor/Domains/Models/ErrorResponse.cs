#nullable enable

using System;

namespace UniCortex.Editor.Domains.Models
{
    [Serializable]
    public class ErrorResponse
    {
        public string error;

        public ErrorResponse(string error)
        {
            this.error = error;
        }
    }
}
