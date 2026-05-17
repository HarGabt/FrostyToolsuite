using Frosty.Core.Controls.Editors;
using Frosty.Core;
using FrostySdk.Attributes;
using FrostySdk.IO;
using MeshSetPlugin.Editors;
using Frosty.Core.Controls;

namespace AssetBankPlugin
{
    [DisplayName("Animation Options")]
    public class AnimationOptions : OptionsExtension
    {
        [Category("Export/Import")]
        [DisplayName("Export Skeleton")]
        [Description("Determines the default skeleton selected when exporting animations.")]
        [Editor(typeof(FrostySkeletonEditor))]
        public string ExportSkeletonAsset { get; set; }

        [Category("Export/Import")]
        [DisplayName("Use Cache")]
        [Description("Whether or not to use the AntRef cache to drastically improve exporting speeds.")]
        [Editor(typeof(FrostyBooleanEditor))]
        public bool UseCache { get; set; }

        public override void Load()
        {
            ExportSkeletonAsset = Config.Get("AnimationExportSkeleton", "", ConfigScope.Game);
            UseCache = Config.Get("UseCache", true, ConfigScope.Game);
        }

        public override void Save()
        {
            Config.Add("AnimationExportSkeleton", ExportSkeletonAsset, ConfigScope.Game);
            Config.Add("UseCache", UseCache, ConfigScope.Game);
            Config.Save();
        }

        public override bool Validate() { return true; }
    }
}
