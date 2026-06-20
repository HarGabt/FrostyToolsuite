using BlueprintEditorPlugin.Models.Entities.Networking;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ObjectReference
{
    /// <summary>
    /// Base implementation for a node which references an external file, and gets it's inputs from it
    /// </summary>
    public abstract class BaseReferenceEntityData : EntityNode
    {
        public override Realm Realm => Realm.Any;

        public override void OnCreation()
        {
            base.OnCreation();
            
            UpdateRef();
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            if (args.Item.Name == "Blueprint")
            {
                // Clear out our connections
                NodeWrangler.ClearConnections(this);
                
                UpdateRef();
            }
        }

        /// <summary>
        /// Assigns inputs & outputs when the Object changes
        /// </summary>
        protected abstract void UpdateRef();
    }
}