using UnityEditor;

namespace EditorBridge.Editor.Settings
{
    [FilePath("EditorBridge/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal sealed class EditorBridgeSettings : ScriptableSingleton<EditorBridgeSettings>
    {
        public int Port = 56780;
        public bool AutoStart = true;

        public void Save()
        {
            Save(true);
        }
    }
}
