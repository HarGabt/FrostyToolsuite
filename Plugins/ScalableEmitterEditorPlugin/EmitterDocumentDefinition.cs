using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ScalableEmitterEditorPlugin
{
    public class EmitterDocumentDefinition : AssetDefinition
    {

        protected static ImageSource emitterSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;Component/Images/Assets/EmitterFileType.png") as ImageSource;

        public EmitterDocumentDefinition()
        {
        }

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new EmitterDocumentEditor(logger);
        }

        public override ImageSource GetIcon()
        {
            return emitterSource;
        }

    }
}
