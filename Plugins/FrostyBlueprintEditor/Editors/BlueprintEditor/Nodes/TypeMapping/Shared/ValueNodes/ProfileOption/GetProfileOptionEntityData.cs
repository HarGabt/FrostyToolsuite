using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostyEditor;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.ProfileOption
{
	public class GetProfileOptionNode : EntityNode
	{
		public override string ObjectType => "GetProfileOptionEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddOutput("IntValue", ConnectionType.Property, Realm);
			AddOutput("FloatValue", ConnectionType.Property, Realm);
			AddOutput("BoolValue", ConnectionType.Property, Realm);
		}

		public override void BuildFooter()
		{
			PointerRef pointerRef = (PointerRef)TryGetProperty("OptionData");
			if (pointerRef != PointerRefType.External)
				return;

			EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(pointerRef.External.FileGuid);
			Footer = $"Option: {assetEntry.Filename}";
		}
	}
}
