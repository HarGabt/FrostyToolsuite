using Frosty.Core.Attributes;
using System.Runtime.InteropServices;
using System.Windows;
using FontPlugin;

[assembly: ComVisible(false)]
[assembly: Guid("4b612468-9b6a-4304-88a5-055c3575eb3d")]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

[assembly: PluginDisplayName("Font Editor")]
[assembly: PluginAuthor("WiiMaster")]
[assembly: PluginVersion("1.0.0.0")]

// Register your menu extension or editor
[assembly: RegisterAssetDefinition("RimeTtfAsset", typeof(RimeTtfAssetDefition))] // Register the asset definition
