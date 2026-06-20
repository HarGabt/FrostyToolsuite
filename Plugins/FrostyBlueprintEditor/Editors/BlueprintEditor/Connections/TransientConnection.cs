using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostyEditor;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    /// <summary>
    /// This connection's data isn't saved, and as a result is purely visual. 
    /// </summary>
    public class TransientConnection : EntityConnection
    {
        public override ConnectionType Type { get; }

        public override void Edit()
        {
            App.Logger.LogError("This connection is transient, and cannot be edited. Sorry!");
        }

        public TransientConnection(IPort source, IPort target, ConnectionType type) : base(source, target)
        {
            Type = type;
        }
    }
}