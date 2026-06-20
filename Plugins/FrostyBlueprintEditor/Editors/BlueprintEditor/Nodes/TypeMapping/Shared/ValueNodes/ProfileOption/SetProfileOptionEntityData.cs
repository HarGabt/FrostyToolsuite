using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using FrostyEditor;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.ProfileOption
{
	public class SetProfileOptionNode : EntityNode
	{
		public override string ObjectType => "SetProfileOptionEntityData";

		public override void OnCreation()
		{
			base.OnCreation();

			AddInput("IntValue", ConnectionType.Property, Realm);
			AddInput("FloatValue", ConnectionType.Property, Realm);
			AddInput("BoolValue", ConnectionType.Property, Realm);
			AddInput("Apply", ConnectionType.Event, Realm);
			AddInput("Save", ConnectionType.Event, Realm);
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
