using Frosty.Core.Attributes;
using System.Runtime.InteropServices;
using System.Windows;
using BlueprintEditorPlugin.Attributes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions;
using BlueprintEditorPlugin.Extensions;
using BlueprintEditorPlugin.Options;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page, 
                                              // app, or any theme specific resource dictionaries)
)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4b612468-9b6a-4304-88a5-055c3575eb3d")]

[assembly: PluginDisplayName("Blueprint Editor")]
[assembly: PluginAuthor("Blueprint Editor Team")]
[assembly: PluginVersion("1.0.0")]

[assembly: RegisterDataExplorerContextMenu(typeof(ViewBlueprintContextMenuItem))]
[assembly: RegisterOptionsExtension(typeof(GraphEditorOptions))]
[assembly: RegisterStartupAction(typeof(BlueprintEditorStartupAction))]
[assembly: RegisterMenuExtension(typeof(ViewHashingUtils))]

[assembly: RegisterBlueprintMenuExtension(typeof(AlignHorizontallyExtension))]
[assembly: RegisterBlueprintMenuExtension(typeof(AlignVerticallyExtension))]
[assembly: RegisterBlueprintMenuExtension(typeof(DistributeHorizontallyMenuExtension))]
[assembly: RegisterBlueprintMenuExtension(typeof(DistributeVerticallyMenuExtension))]
[assembly: RegisterBlueprintMenuExtension(typeof(GenerateBoilerPlateExtension))]
[assembly: RegisterBlueprintMenuExtension(typeof(SelectAssetMenuExtension))]