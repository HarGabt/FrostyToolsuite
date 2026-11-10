using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Frosty.Controls;
using System.Windows.Media;
using System.Linq;

namespace ScalableEmitterEditorPlugin
{
    [TemplatePart(Name = PART_EmitterStackPanel, Type = typeof(FrostyDockablePanel))]
    [TemplatePart(Name = PART_EmitterStack, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PART_EmitterStackColumn, Type = typeof(ColumnDefinition))]
    [TemplatePart(Name = PART_EmitterQualityLowText, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_EmitterQualityLow, Type = typeof(RadioButton))]
    [TemplatePart(Name = PART_EmitterQualityMedium, Type = typeof(RadioButton))]
    [TemplatePart(Name = PART_EmitterQualityHigh, Type = typeof(RadioButton))]
    [TemplatePart(Name = PART_EmitterQualityUltra, Type = typeof(RadioButton))]
    public class EmitterDocumentEditor : FrostyAssetEditor
    {

        #region -- Part Names --

        private const string PART_AssetPropertyGrid = "PART_AssetPropertyGrid";
        private const string PART_EmitterStackPanel = "PART_EmitterStackPanel";
        private const string PART_EmitterStack = "PART_EmitterStack";
        private const string PART_EmitterStackColumn = "PART_EmitterStackColumn";

        private const string PART_EmitterQualityLowText = "PART_EmitterQualityLowText";
        private const string PART_EmitterQualityLow = "PART_EmitterQualityLow";
        private const string PART_EmitterQualityMedium = "PART_EmitterQualityMedium";
        private const string PART_EmitterQualityHigh = "PART_EmitterQualityHigh";
        private const string PART_EmitterQualityUltra = "PART_EmitterQualityUltra";

        #endregion

        #region -- Parts --

        private FrostyPropertyGrid pgAsset;
        private FrostyDockablePanel emitterStackPanel;
        private ItemsControl emitterStack;
        private ColumnDefinition emitterStackColumn;

        private TextBlock emitterQualityLowText;
        private RadioButton emitterQualityLow;
        private RadioButton emitterQualityMedium;
        private RadioButton emitterQualityHigh;
        private RadioButton emitterQualityUltra;

        #endregion

        public ObservableCollection<EmitterStackItemData> EmitterStackItems { get; set; }

        #region -- Constructors --

        static EmitterDocumentEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EmitterDocumentEditor), new FrameworkPropertyMetadata(typeof(EmitterDocumentEditor)));
        }

        public EmitterDocumentEditor()
            : base(null)
        {
            EmitterStackItems = new ObservableCollection<EmitterStackItemData>();
        }

        public EmitterDocumentEditor(ILogger inLogger)
            : base(inLogger)
        {
            EmitterStackItems = new ObservableCollection<EmitterStackItemData>();
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            pgAsset = GetTemplateChild(PART_AssetPropertyGrid) as FrostyPropertyGrid;
            pgAsset.OnModified += PgAsset_OnModified;
            emitterStackPanel = GetTemplateChild(PART_EmitterStackPanel) as FrostyDockablePanel;
            emitterStackPanel.MouseLeftButtonDown += EmitterStackPanel_MouseLeftButtonDown;
            emitterStack = GetTemplateChild(PART_EmitterStack) as ItemsControl;
            emitterStack.Loaded += EmitterStack_Loaded;
            emitterStack.MouseLeftButtonDown += EmitterStack_MouseLeftButtonDown;
            emitterStack.ItemsSource = EmitterStackItems;
            emitterStackColumn = GetTemplateChild(PART_EmitterStackColumn) as ColumnDefinition;
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            emitterQualityLowText = GetTemplateChild(PART_EmitterQualityLowText) as TextBlock;
            emitterQualityLow = GetTemplateChild(PART_EmitterQualityLow) as RadioButton;
            emitterQualityLow.Checked += LowButton_Click;
            emitterQualityMedium = GetTemplateChild(PART_EmitterQualityMedium) as RadioButton;
            emitterQualityMedium.Checked += MediumButton_Click;
            emitterQualityHigh = GetTemplateChild(PART_EmitterQualityHigh) as RadioButton;
            emitterQualityHigh.Checked += HighButton_Click;
            emitterQualityUltra = GetTemplateChild(PART_EmitterQualityUltra) as RadioButton;
            emitterQualityUltra.Checked += UltraButton_Click;
            UpdateToolbar();

            Loaded += EmitterDocumentEditor_Loaded;
            
        }

        private bool firstLoad = true;

        private void EmitterDocumentEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (firstLoad)
            {
                dynamic obj = asset.RootObject;
                firstLoad = false;
                GetEmitterProcessors(obj.TemplateDataLow.Internal);
                activeQualityLevel = new bool[] { true, false, false, false };
                emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

                EmitterStackItems[0].ProcessorSelected = true;
                pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
            }
        }

        void GetEmitterProcessors(dynamic obj)
        {
            dynamic proc = obj;
            EmitterStackItems.Clear();

            // add emitter base
            EmitterStackItems.Add(new EmitterStackItemData(proc, true, pgAsset, new Action<object>((_) => PgAsset_OnModified(null, null))));
            proc = proc.RootProcessor.Internal;

            // add root processor
            if (proc != null)
            {
                EmitterStackItems.Add(new EmitterStackItemData(proc, false, pgAsset, new Action<object>((_) => PgAsset_OnModified(null, null))));
                proc = proc.NextProcessor.Internal;
                while (proc != null)
                {
                    EmitterStackItems.Add(new EmitterStackItemData(proc, false, pgAsset, new Action<object>((_) => PgAsset_OnModified(null, null))));
                    proc = proc.NextProcessor.Internal;
                }
            }
        }

        #region -- Control Events --

        private void PgAsset_OnModified(object sender, ItemModifiedEventArgs e)
        {
            // remember selected item
            dynamic selectedItem = EmitterStackItems.FirstOrDefault(i => i.ProcessorSelected == true)?.EmitterItemObj;
            int selectedId = selectedItem?.__InstanceGuid?.InternalId ?? -1;

            EmitterStackItems.Clear();
            dynamic obj = asset.RootObject;

            if (activeQualityLevel[0])
            {
                GetEmitterProcessors(obj.TemplateDataLow.Internal);
            }
            else if (activeQualityLevel[1])
            {
                GetEmitterProcessors(obj.TemplateDataMedium.Internal);
            }
            else if (activeQualityLevel[2])
            {
                GetEmitterProcessors(obj.TemplateDataHigh.Internal);
            }
            else if (activeQualityLevel[3])
            {
                GetEmitterProcessors(obj.TemplateDataUltra.Internal);
            }

            try {
                EmitterStackItems.FirstOrDefault(o => ((dynamic)o)?.EmitterItemObj?.__InstanceGuid?.InternalId == selectedId).ProcessorSelected = true;
            }
            catch {
                //EmitterStackItems[0].ProcessorSelected = true;
            }
        }

        private void UpdateToolbar()
        {
            dynamic obj = asset.RootObject;

            if (obj.TemplateDataLow == obj.TemplateDataMedium)
            {
                emitterQualityMedium.Visibility = Visibility.Collapsed;
            }


            if (obj.TemplateDataMedium == obj.TemplateDataHigh)
            {
                emitterQualityHigh.Visibility = Visibility.Collapsed;
            }


            if (obj.TemplateDataHigh == obj.TemplateDataUltra)
            {
                emitterQualityUltra.Visibility = Visibility.Collapsed;
            }

            if (emitterQualityMedium.Visibility == Visibility.Collapsed && emitterQualityHigh.Visibility == Visibility.Collapsed && emitterQualityUltra.Visibility == Visibility.Collapsed)
            {
                emitterQualityLowText.Text = "A";
                emitterQualityLow.ToolTip = "All";
            }
        }

        private void EmitterStackPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject visualHit = VisualTreeHelper.HitTest(emitterStackPanel, e.GetPosition(emitterStackPanel)).VisualHit;
            while (visualHit != emitterStack && visualHit != null)
            {
                visualHit = VisualTreeHelper.GetParent(visualHit);
            }

            if (visualHit == emitterStack)
            {
                pgAsset.Object = 0;
                pgAsset.Object = null;
                //logger.Log("Clicked emitter stack");

                for (int i = 0; i < emitterStack.Items.Count; i++)
                {
                    // go down the visual tree into the UniformGrid
                    UIElement stackItemParent = (UIElement)emitterStack.ItemContainerGenerator.ContainerFromIndex(i);
                    for (int k = 0; k < 4; k++)
                    {
                        stackItemParent = VisualTreeHelper.GetChild(stackItemParent, 0) as UIElement;
                    }

                    // go down the visual tree into the processor block
                    UIElement proc = stackItemParent;
                    for (int k = 0; k < 2; k++)
                    {
                        proc = VisualTreeHelper.GetChild(proc, 1 - k) as UIElement;
                    }

                    // go down the visual tree into the evaluator block
                    UIElement eval = stackItemParent;
                    for (int k = 0; k < 2; k++)
                    {
                        eval = VisualTreeHelper.GetChild(eval, 0) as UIElement;
                    }

                    if (proc != null)
                    {
                        if (proc.IsMouseOver)
                        {
                            EmitterStackItems[i].ProcessorSelected = true;
                            pgAsset.SetClass(EmitterStackItems[i].EmitterItemObj);
                            //logger.Log("Processor selected");
                        }
                        else
                        {
                            EmitterStackItems[i].ProcessorSelected = false;
                            //logger.Log("Processor deselected");
                        }
                    }
                    if (eval != null)
                    {
                        if (eval.IsMouseOver)
                        {
                            EmitterStackItems[i].EvaluatorSelected = true;
                            pgAsset.SetClass(EmitterStackItems[i].EvaluatorObj);
                            //logger.Log("Evaluator selected");
                        }
                        else
                        {
                            EmitterStackItems[i].EvaluatorSelected = false;
                            //logger.Log("Evaluator deselected");
                        }
                    }
                }
            }
            else
            {
                // stop the property grid from being cleared if there isn't a stack being displayed
                if (EmitterStackItems.Count > 0)
                {
                    foreach (EmitterStackItemData item in EmitterStackItems)
                    {
                        item.ProcessorSelected = false;
                        item.EvaluatorSelected = false;
                    }
                    EmitterStackItems[0].ProcessorSelected = true;
                    pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
                }
            }
        }

        private void EmitterStack_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void EmitterStack_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ///logger.Log("Clicked emitter stack");
            ///for (int i = 0; i < emitterStack.Items.Count; i++)
            ///{
            ///    //ContentPresenter c = (ContentPresenter)emitterStack.ItemContainerGenerator.ContainerFromItem(emitterStack.Items[i]);
            ///    //Border proc = c.ContentTemplate.FindName("PART_ProcessorBox", c) as Border;
            ///    Border proc = ItemsControlHelpers.findElementInItemsControlItemAtIndex<Border>(emitterStack, i, "PART_ProcessorBox") as Border;
            ///    Border eval = ItemsControlHelpers.findElementInItemsControlItemAtIndex<Border>(emitterStack, i, "PART_EvaluatorBox") as Border;
            ///    //Border eval = c.ContentTemplate.FindName("PART_EvaluatorBox", c) as Border;
            ///
            ///    if (proc != null)
            ///    {
            ///        if (proc.IsMouseOver)
            ///        {
            ///            EmitterStackItems[i].ProcessorSelected = true;
            ///            logger.Log("Processor selected");
            ///        }
            ///        else
            ///        {
            ///            EmitterStackItems[i].ProcessorSelected = false;
            ///            logger.Log("Processor deselected");
            ///        }
            ///    }
            ///    if (eval != null)
            ///    {
            ///        if (eval.IsMouseOver)
            ///        {
            ///            EmitterStackItems[i].EvaluatorSelected = true;
            ///            logger.Log("Evaluator selected");
            ///        }
            ///        else
            ///        {
            ///            EmitterStackItems[i].EvaluatorSelected = false;
            ///            logger.Log("Evaluator deselected");
            ///        }
            ///    }
            ///
            ///}
            ///EmitterStackItem stackItem = emitterStack.InputHitTest(e.GetPosition(emitterStack)) as EmitterStackItem;
            ///if (stackItem != null)
            ///{
            ///    emitterStack.Items.Contains(stackItem);
            ///    logger.Log("passed hit test");
            ///}
        }

        #endregion

        #region -- Toolbar --

        bool[] activeQualityLevel = { true, false, false, false };

        public override List<ToolbarItem> RegisterToolbarItems()
        {
            return new List<ToolbarItem>()
            {
                new ToolbarItem("Show All", "", "", new RelayCommand((object state) => { ShowAllButton_Click(this, new RoutedEventArgs()); })),
                new ToolbarItem("Show Editor", "", "", new RelayCommand((object state) => { LowButton_Click(this, new RoutedEventArgs()); })),
                new ToolbarItem("Unify", "Unify Qualities", "", new RelayCommand((object state) => { UnifyButton_Click(this, new RoutedEventArgs()); })),
            };
        }

        private void UnifyButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = asset.RootObject;
            obj.TemplateDataLow = obj.TemplateDataUltra;
            obj.TemplateDataMedium = obj.TemplateDataUltra;
            obj.TemplateDataHigh = obj.TemplateDataUltra;
            UpdateToolbar();
            AssetModified = true;
            InvokeOnAssetModified();

            Application.Current.Dispatcher.BeginInvoke(new Action(() => emitterQualityLow.IsChecked = true));
            LowButton_Click(this, new RoutedEventArgs());
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            pgAsset.Object = asset.RootObject;
            EmitterStackItems.Clear();
            activeQualityLevel = new bool[] { false, false, false, false };
            emitterStackColumn.Width = new GridLength(0);
        }

        private void LowButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = asset.RootObject;
            GetEmitterProcessors(obj.TemplateDataLow.Internal);
            activeQualityLevel = new bool[]{ true, false, false, false };
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            EmitterStackItems[0].ProcessorSelected = true;
            pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
        }

        private void MediumButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = asset.RootObject;
            GetEmitterProcessors(obj.TemplateDataMedium.Internal);
            activeQualityLevel = new bool[] { false, true, false, false };
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            EmitterStackItems[0].ProcessorSelected = true;
            pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
        }

        private void HighButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = asset.RootObject;
            GetEmitterProcessors(obj.TemplateDataHigh.Internal);
            activeQualityLevel = new bool[] { false, false, true, false };
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            EmitterStackItems[0].ProcessorSelected = true;
            pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
        }

        private void UltraButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = asset.RootObject;
            GetEmitterProcessors(obj.TemplateDataUltra.Internal);
            activeQualityLevel = new bool[] { false, false, false, true };
            emitterStackColumn.Width = new GridLength(2, GridUnitType.Star);

            EmitterStackItems[0].ProcessorSelected = true;
            pgAsset.SetClass(EmitterStackItems[0].EmitterItemObj);
        }

        #endregion

    }
}
