using System.Reflection;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.StarWarsBattlefrontII.AutoPlayers
{
    public class AutoPlayerObjectiveNode : EntityNode
    {
        public override string ObjectType => "AutoPlayerObjectiveEntityData";

        public override bool IsValid()
        {
            return ProfilesLibrary.ProfileName == "starwarsbattlefrontii" || ProfilesLibrary.ProfileName == "bfv";
        }

        public override void OnCreation()
        {
            base.OnCreation();
            
            // I am too lazy to add all of these inputs by hand
            foreach (PropertyInfo property in Object.GetType().GetProperties())
            {
                // These cannot have inputs
                if (!property.PropertyType.IsPrimitive && !property.PropertyType.IsValueType || property.PropertyType.IsEnum)
                    continue;
                
                if (property.Name == "__Id" || property.Name == "__InstanceGuid" || property.Name == "Flags" || property.Name == "Realm")
                    continue;

                AddInput(property.Name, ConnectionType.Property, Realm);
            }

            AddInput("Players", ConnectionType.Property, Realm);
            AddInput("Activate", ConnectionType.Event, Realm);

            AddOutput("OnActivated", ConnectionType.Event, Realm, true);
        }
    }
}