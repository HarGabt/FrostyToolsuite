using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Comparison
{
    public class InclusionSettingNode : EntityNode
    {
        public override string ObjectType => "InclusionSettingEntityData";
        public override string ToolTip => "This node is used to determine session parameters such as Gamemode Id";

        public override void BuildFooter()
        {
            if (TryGetProperty("Setting") != null && TryGetProperty("Settings") != null)
            {
                string settingName = ((CString)TryGetProperty("Setting")).ToString();
                List<CString> settings = ((List<CString>)TryGetProperty("Settings"));
                Footer = $"{settingName}:\t{string.Join(",", settings.Select(setName => $"\"{setName}\""))}";
            }
        }
        public override void OnCreation()
        {
            base.OnCreation();

            AddOutput("Out", ConnectionType.Property);
        }
    }
}