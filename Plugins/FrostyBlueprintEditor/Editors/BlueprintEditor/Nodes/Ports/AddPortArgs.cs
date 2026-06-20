using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostySdk.Attributes;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public class AddPortArgs : EditPortArgs
    {
        [Category("Basic Details")]
        [Description("The direction this port should face. Otherwise known as Input/Output")]
        public PortDirection Direction { get; set; }
        
        [Category("Entity Details")]
        [DisplayName("Type")]
        [Description("The type of connection this port should allow")]
        public ConnectionType ConnectionType { get; set; }
        
        public AddPortArgs()
        {
            Name = "";
            Realm = Realm.Any;
        }
    }
}