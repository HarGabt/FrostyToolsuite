using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.BattleAI
{
    public class AiTemplateFilterNode : EntityNode
    {
        public override string ObjectType => "AITemplateFilterEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("0xca2039c7", ConnectionType.Event, Realm); // TODO: Solve hash
        }

        public override void BuildFooter()
        {
            ClearFooter();
            dynamic templates = TryGetProperty("Templates");
            if (templates == null)
                return;
            
            foreach (CString template in templates)
            {
                if (template.IsNull())
                    continue;
                
                AddFooter(template.ToString());
            }
        }
    }
}