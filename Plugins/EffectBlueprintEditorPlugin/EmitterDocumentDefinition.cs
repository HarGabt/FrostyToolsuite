using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EffectBlueprintEditorPlugin
{
    public class EmitterDocumentDefinition : AssetDefinition
    {

        protected static ImageSource emitterSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/EffectBlueprintEditorPlugin;component/Images/EffectEditor.png") as ImageSource;

        public EmitterDocumentDefinition()
        {
        }

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new EffectBlueprintEditor(logger);
        }

        public override ImageSource GetIcon()
        {
            return emitterSource;
        }

    }
}
