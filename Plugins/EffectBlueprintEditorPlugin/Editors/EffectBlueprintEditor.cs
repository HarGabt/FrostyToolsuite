using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Frosty.Controls;
using System.Linq;
using System.Windows.Controls.Primitives;
using FrostySdk.Ebx;

namespace EffectBlueprintEditorPlugin {
    [TemplatePart(Name = PART_EmitterStackPanel, Type = typeof(FrostyDockablePanel))]
    [TemplatePart(Name = PART_EmitterStack, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PART_EmitterStackColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PART_Refresh, Type = typeof(Button))]
    [TemplatePart(Name = PART_AutoRefresh, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PART_ShowUnknown, Type = typeof(ToggleButton))]
    public class EffectBlueprintEditor : FrostyAssetEditor {

        #region -- Part Names --

        private const string PART_AssetPropertyGrid = "PART_AssetPropertyGrid";
        private const string PART_EmitterStackPanel = "PART_EmitterStackPanel";
        private const string PART_EmitterStack = "PART_EmitterStack";
        private const string PART_EmitterStackColumn = "PART_EmitterStackColumn";
        private const string PART_Refresh = "PART_Refresh";
        private const string PART_AutoRefresh = "PART_AutoRefresh";

        private const string PART_ShowEM = "PART_ShowEM";
        private const string PART_ShowEG = "PART_ShowEG";
        private const string PART_ShowLE = "PART_ShowLE";

        private const string PART_ShowTransforms = "PART_ShowTransforms";
        private const string PART_ShowDelay = "PART_ShowDelay";
        private const string PART_ShowAttr = "PART_ShowAttr";
        private const string PART_ShowGrOverrides = "PART_ShowGrOverrides";
        private const string PART_ShowUnknown = "PART_ShowUnknown";

        private const string PART_RefreshTime = "PART_RefreshTime";

        #endregion

        #region -- Parts --

        private FrostyPropertyGrid pgAsset;
        private FrostyDockablePanel emitterStackPanel;
        private ItemsControl emitterStack;
        private ColumnDefinition emitterStackColumn;
        private Button refreshButton;
        private ToggleButton autoRefreshButton;
        private ToggleButton showEMButton;
        private ToggleButton showEGButton;
        private ToggleButton showLEButton;
        private ToggleButton showTransformsButton;
        private ToggleButton showDelayButton;
        private ToggleButton showAttributesButton;
        private ToggleButton showGraphOverridesButton;
        private ToggleButton showUnknownButton;
        private TextBlock refreshTimeText;

        #endregion

        private bool showEditor = true;

        public ObservableCollection<EffectStackItemData> EmitterStackItems { get; set; }

        Dictionary<string, Dictionary<int, string[]>> VSF = [];

        private Action<object> refreshPropertyGrid;
        private Action<object> refreshEffectStack;

        #region -- Constructors --

        static EffectBlueprintEditor() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EffectBlueprintEditor), new FrameworkPropertyMetadata(typeof(EffectBlueprintEditor)));
        }

        public EffectBlueprintEditor()
            : base(null) {
            EmitterStackItems = [];
        }

        public EffectBlueprintEditor(ILogger inLogger)
            : base(inLogger) {
            EmitterStackItems = [];
        }

        #endregion

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            pgAsset = GetTemplateChild(PART_AssetPropertyGrid) as FrostyPropertyGrid;
            pgAsset.OnModified += (s, e) => GetEffectStackItems(asset.RootObject);
            emitterStackPanel = GetTemplateChild(PART_EmitterStackPanel) as FrostyDockablePanel;
            emitterStack = GetTemplateChild(PART_EmitterStack) as ItemsControl;
            emitterStack.Loaded += EmitterStack_Loaded;
            emitterStack.ItemsSource = EmitterStackItems;
            emitterStackColumn = GetTemplateChild(PART_EmitterStackColumn) as ColumnDefinition;
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            refreshButton = GetTemplateChild(PART_Refresh) as Button;
            refreshButton.Click += RefreshButton_Click;

            autoRefreshButton = GetTemplateChild(PART_AutoRefresh) as ToggleButton;

            showEMButton = GetTemplateChild(PART_ShowEM) as ToggleButton;
            showEMButton.Click += (s, e) => refreshEffectStack.Invoke(null);
            showEGButton = GetTemplateChild(PART_ShowEG) as ToggleButton;
            showEGButton.Click += (s, e) => refreshEffectStack.Invoke(null);
            showLEButton = GetTemplateChild(PART_ShowLE) as ToggleButton;
            showLEButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            showTransformsButton = GetTemplateChild(PART_ShowTransforms) as ToggleButton;
            showTransformsButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            showDelayButton = GetTemplateChild(PART_ShowDelay) as ToggleButton;
            showDelayButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            showAttributesButton = GetTemplateChild(PART_ShowAttr) as ToggleButton;
            showAttributesButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            showGraphOverridesButton = GetTemplateChild(PART_ShowGrOverrides) as ToggleButton;
            showGraphOverridesButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            showUnknownButton = GetTemplateChild(PART_ShowUnknown) as ToggleButton;
            showUnknownButton.Click += (s, e) => refreshEffectStack.Invoke(null);

            refreshTimeText = GetTemplateChild(PART_RefreshTime) as TextBlock;

            Loaded += Editor_Loaded;

            refreshPropertyGrid = new Action<object>((_) => {
                pgAsset.Object = asset.RootObject;
                pgAsset.Object = ((dynamic)asset.RootObject).Object.Internal;
            });
            refreshEffectStack = new Action<object>((_) => GetEffectStackItems(asset.RootObject));
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) {
            refreshPropertyGrid.Invoke(null);
            refreshEffectStack.Invoke(null);
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e) {
            dynamic obj = asset.RootObject;

            GetEffectStackItems(obj);

            if (pgAsset.Object != obj.Object.Internal)
                pgAsset.Object = obj.Object.Internal;
        }

        void GetEffectStackItems(dynamic obj) {
            //DateTime startTime = DateTime.Now;
            //emitterStack.ItemsSource = null;
            EmitterStackItems.Clear();

            if (!showEditor) return;
            if (obj?.Object?.Internal is null) return;

            int count = -1;
            foreach (dynamic component in obj.Object.Internal.Components) {
                count++;
                if (component?.Internal is null) continue;

                if (component.Internal.GetType().Name == "EmitterGraphEntityData" && showEGButton.IsChecked) {
                    dynamic reference = App.AssetManager.GetEbxEntry(component.Internal.EmitterGraph.External.FileGuid);

                    // Header
                    EmitterStackItems.Add(new EffectStackItemData(component, $"[{count}] {component.Internal.__Id} - {reference?.DisplayName ?? "Invalid"}", (List<PointerRef>)obj.Object.Internal.Components, pgAsset, refreshPropertyGrid + refreshEffectStack));

                    if (!VSF.TryGetValue(reference.Name, out Dictionary<int, string[]> vsfParams)) {
                        vsfParams = [];
                        try {
                            dynamic eGraph = App.AssetManager.GetEbx(reference).RootObject;

                            dynamic[] egParams = eGraph.EmitterGraphParams.ToArray();

                            string vsf = App.AssetManager.GetEbx(String.IsNullOrEmpty(eGraph.MeshVertexShaderFragmentAssetName) ? eGraph.VertexShaderFragmentAssetName : eGraph.MeshVertexShaderFragmentAssetName).RootObject.PipelineGeneratedSourceCode;

                            string[] lines = vsf.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                            List<string> vsfParamLines = lines.Where(l => l.Contains("g_emitterGraphParams[") && l.Contains("].xyzw")).ToList();

                            for (int i = 0; i < vsfParamLines.Count; i++) {
                                List<string> param = [];
                                foreach (string vsfParamLine in vsfParamLines[i].Split()[3].Split('_')) {
                                    param.Add(char.ToUpper(vsfParamLine[0]) + vsfParamLine.Substring(1));
                                }
                                vsfParams.Add(egParams[i].PropertyId, param.ToArray());
                            }
                        }
                        catch { }
                        VSF.Add(reference.Name, vsfParams);
                    }

                    if (showTransformsButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Transform, pgAsset));

                    if (showDelayButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal, "StartDelay", pgAsset));

                    if (showGraphOverridesButton.IsChecked == true) {
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.EmitterGraphOverrides.SpawnRate, pgAsset, "Spawn Rate"));
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.EmitterGraphOverrides.ParticleMaxCount, pgAsset, "Particle Max Count"));
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.EmitterGraphOverrides.ParticleLifeSpan, pgAsset, "Particle Lifespan"));
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.EmitterGraphOverrides.EmitterLifeSpan, pgAsset, "Emitter Lifespan"));
                    }

                    if (showAttributesButton.IsChecked == true) {
                        foreach (dynamic param in component.Internal.EmitterGraphParams) {
                            if (vsfParams.TryGetValue(param.PropertyId, out string[] _) || showUnknownButton.IsChecked) {
                                EmitterStackItems.Add(new EffectStackItemData(param.PropertyId, param.Value, pgAsset, vsfParams));
                            }
                        }

                        // Add New Param
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal, pgAsset, vsfParams, refreshEffectStack));
                    }
                }

                if (component.Internal.GetType().Name == "LightEffectEntityData" && showLEButton.IsChecked) {
                    // Header
                    EmitterStackItems.Add(new EffectStackItemData(component, $"[{count}] {component.Internal.__Id} - {component.Internal.Light.Internal?.__Id}", (List<PointerRef>)obj.Object.Internal.Components, pgAsset, refreshPropertyGrid + refreshEffectStack));

                    if (showTransformsButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Transform, pgAsset));

                    if (showDelayButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal, "StartDelay", pgAsset));

                    if (component?.Internal?.Light?.Internal is null) continue;

                    if (showAttributesButton.IsChecked == true) {
                        EmitterStackItems.Add(new EffectStackItemData(-1, component.Internal.Light.Internal.Color, pgAsset, new Dictionary<int, string[]> { { -1, new string[] { "Color" } } }));

                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "Intensity", pgAsset));
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "AttenuationRadius", pgAsset));
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "AttenuationOffset", pgAsset));

                        if (component.Internal.Light.Internal.GetType().Name == "PbrTubeLightEntityData") {
                            EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "TubeRadius", pgAsset));
                            EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "TubeWidth", pgAsset));
                        }
                        if (component.Internal.Light.Internal.GetType().Name == "PbrSphereLightEntityData") {
                            //EmitterStackItems.Add(new EffectStackItemData(component.Internal.Light.Internal, "SphereRadius", pgAsset));
                        }
                    }
                }

                if (component.Internal.GetType().Name == "EmitterEntityData" && showEMButton.IsChecked) {
                    dynamic reference = App.AssetManager.GetEbxEntry(component.Internal.Emitter.External.FileGuid);

                    // Header
                    EmitterStackItems.Add(new EffectStackItemData(component, $"[{count}] {component.Internal.__Id} - {reference?.DisplayName ?? "Invalid"}", (List<PointerRef>)obj.Object.Internal.Components, pgAsset, refreshPropertyGrid + refreshEffectStack));

                    if (showTransformsButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal.Transform, pgAsset));

                    if (showDelayButton.IsChecked == true)
                        EmitterStackItems.Add(new EffectStackItemData(component.Internal, "StartDelay", pgAsset));
                }
            }

            //emitterStack.ItemsSource = EmitterStackItems;
            //refreshTimeText.Text = $"{(DateTime.Now - startTime).TotalSeconds}s";
        }

        #region -- Control Events --

        private void EmitterStack_Loaded(object sender, RoutedEventArgs e) {
        }

        #endregion

        #region -- Toolbar --

        public override List<ToolbarItem> RegisterToolbarItems() {
            return
            [
                new("Editor", "Editor View", "", new RelayCommand((object state) => { EnableEditor(); })),
                new("Standard", "Standard View", "", new RelayCommand((object state) => { DisableEditor(); }))
            ];
        }

        private void DisableEditor() {
            showEditor = false;
            pgAsset.Object = asset.RootObject;
            EmitterStackItems.Clear();
            emitterStackColumn.Width = new GridLength(0);
        }

        private void EnableEditor() {
            showEditor = true;
            dynamic obj = asset.RootObject;
            pgAsset.Object = obj.Object.Internal;
            GetEffectStackItems(obj);
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);
        }

        #endregion

    }
}
