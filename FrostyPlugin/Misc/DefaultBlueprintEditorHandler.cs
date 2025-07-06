using Frosty.Controls;
using Frosty.Core.Attributes;
using FrostySdk.Managers.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frosty.Core.Misc
{
    public class DefaultBlueprintEditorHandler : IBlueprintEditorHandler
    {
        public void OpenAssetAsGraph(EbxAssetEntry asset)
        {
            FrostyMessageBox.Show("Missing valid handler for opening blueprint editor. Please make sure you have a version of the blueprint editor that supports this feature.", "Open in Blueprint Editor");
        }
    }
}
