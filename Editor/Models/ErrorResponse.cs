using System;

#nullable enable

namespace EditorBridge.Editor.Models
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
