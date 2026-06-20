using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostySdk.Attributes;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public class EditPortArgs
    {
        [DisplayName("Name")]
        [Category("Basic Details")]
        [Description("The name of this port")]
        public string Name { get; set; }

        [DisplayName("Realm")]
        [Category("Basic Details")]
        [Description("The realm, or machine, this port should run on. For example, if set to Client, connections to this won't get networked or synced. While Server will")]
        public Realm Realm { get; set; }
        
        [Category("Entity Details")]
        [DisplayName("Has PlayerEvent")]
        [Description("Whether or not this has a PlayerEvent. Does nothing if this is not an Event")]
        public bool HasPlayer { get; set; }
        
        [Category("Entity Details")]
        [DisplayName("Is Interface")]
        [Description("This input is part of a Interface. An Interface can typically be determined if this input goes to a InterfaceDescriptor(found in logic or spatial prefabs) or from one asset to another.")]
        public bool IsInterface { get; set; }

        public EditPortArgs(EntityPort port)
        {
            Name = port.Name;
            Realm = port.Realm;
            HasPlayer = port.HasPlayer;
            IsInterface = port.IsInterface;
        }

        public EditPortArgs()
        {
            Name = "";
            Realm = Realm.Any;
        }
    }
}