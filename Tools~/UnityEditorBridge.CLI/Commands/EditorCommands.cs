namespace UnityEditorBridge.CLI.Commands;

public class EditorCommands
{
    /// <summary>Check server connectivity.</summary>
    public static async Task Ping()
    {
        await BridgeClient.GetAsync("/ping");
    }
}
