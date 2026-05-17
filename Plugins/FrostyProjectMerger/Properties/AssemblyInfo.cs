using Frosty.Core.Attributes;
using System.Runtime.InteropServices;
using System.Windows;
using ProjectMerger;

[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

[assembly: Guid("4b612468-9b6a-4304-88a5-055c3575eb3d")]

[assembly: PluginDisplayName("Project Merger")]
[assembly: PluginAuthor("Y wingpilot2")]
[assembly: PluginVersion("1.2.1.0")]
[assembly: RegisterToolbarExtension(typeof(ProjectMergerToolbarExtension))]
