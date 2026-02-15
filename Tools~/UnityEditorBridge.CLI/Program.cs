using ConsoleAppFramework;
using UnityEditorBridge.CLI.Commands;

var app = ConsoleApp.Create();
app.Add<EditorCommands>("editor");
app.Run(args);
