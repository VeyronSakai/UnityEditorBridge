using System;

#nullable enable

namespace EditorBridge.Editor.Models
{
    [Serializable]
    public class PingResponse
    {
        public string status;
        public string message;

        public PingResponse(string status, string message)
        {
            this.status = status;
            this.message = message;
        }
    }
}
