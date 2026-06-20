using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostySdk.Attributes;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class EditConnectionArgs
    {
        public Realm Realm { get; set; }

        public EditConnectionArgs(EntityConnection connection)
        {
            Realm = connection.Realm;
        }

        public EditConnectionArgs()
        {
        }
    }

    public class EditPropConnectionArgs : EditConnectionArgs
    {
        [DisplayName("Property Type")]
        public PropertyType PropertyType { get; set; }
        
        public EditPropConnectionArgs(EntityConnection connection)
        {
            Realm = connection.Realm;
            PropertyType = connection.PropType;
        }

        public EditPropConnectionArgs()
        {
        }
    }
}