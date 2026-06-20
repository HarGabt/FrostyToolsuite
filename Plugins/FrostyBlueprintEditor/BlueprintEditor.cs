using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Extensions;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin
{
    [TemplatePart (Name = ContentPresenter, Type = typeof(ContentPresenter))]
    public class BlueprintEditor : FrostyBaseEditor
    {
        private const string ContentPresenter = "ContentPresenter";
        private ContentPresenter _presenter;
        private IEbxGraphEditor _graphEditor;
        private EbxAssetEntry _assetEntry;
        private bool _hasLoaded;
        public override ImageSource Icon => ViewBlueprintContextMenuItem.IconImageSource;

        static BlueprintEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintEditor), new FrameworkPropertyMetadata(typeof(BlueprintEditor)));
        }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _presenter = GetTemplateChild(ContentPresenter) as ContentPresenter;
            _presenter.Content = _graphEditor;
            
            Loaded += (sender, args) =>
            {
                if (_hasLoaded)
                    return;
                
                _hasLoaded = true;
                InternalLoad();
            };
        }
        
        public void LoadBlueprint(EbxAssetEntry assetEntry, IEbxGraphEditor graphEditor)
        {
            _graphEditor = graphEditor;
            _assetEntry = assetEntry;
            InternalLoad();
        }

        private void InternalLoad()
        {
            _hasLoaded = true;
            FrostyTaskWindow.Show("Loading Blueprint...", "", task =>
            {
                _graphEditor.LoadAsset(_assetEntry);
            });
        }

        public override void Closed()
        {
            base.Closed();
            _graphEditor.Closed();
        }

        public BlueprintEditor()
        {
        }

        public BlueprintEditor(EbxAssetEntry assetEntry, IEbxGraphEditor graphEditor)
        {
            _graphEditor = graphEditor;
            _assetEntry = assetEntry;
        }
    }
}