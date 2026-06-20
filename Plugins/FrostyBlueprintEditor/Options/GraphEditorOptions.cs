using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.GraphEditor;
using Frosty.Core;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Misc;
using FrostySdk.Attributes;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Options
{
    [DisplayName("Control")]
    [Description("A custom control with an action")]
    [EbxClassMeta(EbxFieldType.Struct)]
    public class BlueprintEditorControl
    {
        [Description("The button which when pressed activates this control")]
        [EbxFieldMeta(EbxFieldType.Enum)]
        public Key Key { get; set; }
        
        [DisplayName("Use Modifier Key")]
        [Description("Whether or not to use a Modifier Key")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool UseModifierKey { get; set; }
        
        [DisplayName("Modifier Key")]
        [Description("The modifier key held down when this action occurs")]
        [DependsOn("UseModifierKey")]
        [EbxFieldMeta(EbxFieldType.Enum)]
        public ModifierKeys ModifierKey { get; set; }
        
        [DisplayName("Node Name")]
        [Description("The name of the node to place when this control is activated")]
        public string TypeName { get; set; }

        public override string ToString()
        {
            return UseModifierKey ? $"{Key} places {TypeName}" : $"{ModifierKey} + {Key} places {TypeName}";
        }

        public BlueprintEditorControl()
        {
            UseModifierKey = true;
            ModifierKey = ModifierKeys.Alt;
            Key = Key.C;
            TypeName = "";
        }
    }
    
    public enum ConnectionStyle
    {
        Curvy = 0,
        Straight = 1,
        StartStop = 2
    }
    
    public class ConnectionStyleCombo : FrostyCustomComboDataEditor<ConnectionStyle, string>
    {
    }
    
    [DisplayName("Graph Editor Options")]
    [Description("Overall options for Graph Editors provided by Blueprint Editor")]
    public class GraphEditorOptions : OptionsExtension
    {
        private List<ConnectionStyle> _styles = new List<ConnectionStyle>()
        {
            ConnectionStyle.Curvy,
            ConnectionStyle.Straight,
            ConnectionStyle.StartStop
        };
        private List<string> _styleNames = new List<string>()
        {
            "Curvy",
            "Straight",
            "Start-Stop"
        };

        #region Graph Editor

        [Category("Connections")]
        [DisplayName("Connection Style")]
        [EbxFieldMeta(EbxFieldType.Struct)]
        [Editor(typeof(ConnectionStyleCombo))]
        public CustomComboData<ConnectionStyle, string> CStyle { get; set; }
        
        [Category("Connections")]
        [DisplayName("Thickness")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(1.0f, 8.0f, 0.1f, 2.0f, true)]
        public float WireThickness { get; set; }
        
        [Category("Connections")]
        [DisplayName("Connections over Nodes")]
        [Description("Whether or not to display connections on top of nodes")]
        public bool WiresOverVerts { get; set; }
        
        [Category("Ports")]
        [DisplayName("Size")]
        [Description("How large a port should be")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(1.0f, 10.0f, 0.1f, 2.0f, true)]
        public float PortSize { get; set; }
        
        [Category("Ports")]
        [DisplayName("Location")]
        [Description("Where on the horizontal axis of the node should the port be located. 0 is inside, 10 is outside")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(0.0f, 10.0f, 0.1f, 2.0f, true)]
        public float PortPosition { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Redirect Cycles")]
        [Description("EXPERIMENTAL!\nCreates redirects for cyclical graphs, thus removing loops(e.g a->b, b->c, c->a)")]
        public bool AutoRedirects { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Horizontal padding")]
        [Description("The amount of horizontal spacing between each node to maintain whenever automatically sorting")]
        public float VertXSpacing { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Vertical padding")]
        [Description("The amount of vertical spacing between each node to maintain whenever automatically sorting")]
        public float VertYSpacing { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Save on close")]
        [Description("Saves a layout file whenever closing a graph editor")]
        public bool SaveLayoutExit { get; set; }
        
        [Category("Editor")]
        [DisplayName("Load before Opening")]
        [Description("If true, this will load the EBX before opening the graph editor itself, similar to how normally opening assets works.")]
        public bool LoadBeforeOpen { get; set; }

        #endregion

        #region Blueprint Editor

        [DisplayName("Node Shortcuts")]
        [Category("Controls")]
        [Description("A list of shortcuts for placing down nodes")]
        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<BlueprintEditorControl> EditorControls { get; set; } = new List<BlueprintEditorControl>();

        #endregion

        public override void Load()
        {
            CStyle = new CustomComboData<ConnectionStyle, string>(_styles, _styleNames);
            switch (Config.Get("ConnectionStyle", "StartStop"))
            {
                case "StartStop":
                {
                    CStyle.SelectedIndex = 2;
                } break;
                case "Straight":
                {
                    CStyle.SelectedIndex = 1;
                } break;
                case "Curvy":
                {
                    CStyle.SelectedIndex = 0;
                } break;
            }

            WireThickness = Config.Get("WireThickness", 4.0f);
            WiresOverVerts = Config.Get("WireOverVert", false);
            
            PortSize = Config.Get("PortSize", 6.0f);
            PortPosition = Config.Get("PortPos", 0.0f);
            
            AutoRedirects = Config.Get("AutoRedirects", false);
            VertXSpacing = Config.Get("VertXSpacing", 64.0f);
            VertYSpacing = Config.Get("VertYSpacing", 16.0f);

            SaveLayoutExit = Config.Get("SaveLayoutOnExit", true);
            
            LoadBeforeOpen = Config.Get("LoadBeforeOpen", false);
            
            List<BlueprintEditorControl> controls = Config.Get("BlueprintEditorControls", new List<BlueprintEditorControl>(), ConfigScope.Game);
            foreach (BlueprintEditorControl control in controls)
            {
                if (!ControlIsValid(control))
                    continue;
                
                EditorControls.Add(control);
            }
        }

        public override void Save()
        {
            switch (CStyle.SelectedValue)
            {
                case ConnectionStyle.Curvy:
                {
                    Config.Add("ConnectionStyle", "Curvy");
                } break;
                case ConnectionStyle.Straight:
                {
                    Config.Add("ConnectionStyle", "Straight");
                } break;
                case ConnectionStyle.StartStop:
                {
                    Config.Add("ConnectionStyle", "StartStop");
                } break;
            }
            
            Config.Add("WireThickness", WireThickness);
            Config.Add("WireOverVert", WiresOverVerts);
            
            Config.Add("PortSize", PortSize);
            Config.Add("PortPos", PortPosition);
            
            Config.Add("AutoRedirects", AutoRedirects);
            Config.Add("VertXSpacing", VertXSpacing);
            Config.Add("VertYSpacing", VertYSpacing);
            
            Config.Add("SaveLayoutOnExit", SaveLayoutExit);
            
            Config.Add("LoadBeforeOpen", LoadBeforeOpen);
            
            Config.Add("BlueprintEditorControls", EditorControls, ConfigScope.Game);
            
            EditorOptions.Update();
        }
        
        public override bool Validate()
        {
            Dictionary<string, BlueprintEditorControl> validControls = new Dictionary<string, BlueprintEditorControl>();

            foreach (BlueprintEditorControl control in EditorControls)
            {
                if (validControls.ContainsKey(control.TypeName))
                {
                    App.Logger.LogError("Cannot have multiple keybindings to the same node");
                    return false;
                }

                if (validControls.Any(c =>
                        c.Value.Key == control.Key &&
                        (c.Value.ModifierKey == control.ModifierKey && c.Value.UseModifierKey)))
                {
                    App.Logger.LogError("Cannot have multiple of the same keybinding");
                    return false;
                }
                
                validControls.Add(control.TypeName, control);
            }
            
            return true;
        }

        private bool ControlIsValid(BlueprintEditorControl control)
        {
            foreach (BlueprintEditorControl editorControl in EditorControls)
            {
                if (editorControl.TypeName != control.TypeName)
                    continue;

                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// This class stores all of the users options for the <see cref="IGraphEditor"/>
    /// </summary>
    public static class EditorOptions
    {
        public static ConnectionStyle WireStyle { get; internal set; }
        public static double WireThickness { get; internal set; }
        public static bool WiresOververts { get; internal set; }
        
        public static double PortSize { get; internal set; }
        public static double InputPos { get; internal set; }
        public static double OutputPos { get; internal set; }

        public static bool RedirectCycles { get; internal set; }
        public static double VertXSpacing { get; internal set; }
        public static double VertYSpacing { get; internal set; }
        
        public static bool SaveOnExit { get; internal set; }
        
        public static bool LoadBeforeOpen { get; internal set; }

        public static void Update()
        {
            switch (Config.Get("ConnectionStyle", "StartStop"))
            {
                case "StartStop":
                {
                    WireStyle = ConnectionStyle.StartStop;
                } break;
                case "Straight":
                {
                    WireStyle = ConnectionStyle.Straight;
                } break;
                case "Curvy":
                {
                    WireStyle = ConnectionStyle.Curvy;
                } break;
            }
            
            WireThickness = Config.Get("WireThickness", 4.0f);
            WiresOververts = Config.Get("WireOverVert", false);
            
            PortSize = (Config.Get("PortSize", 6.0f) * 0.1) * 15;
            OutputPos = Config.Get("PortPos", 0.0f);
            InputPos = OutputPos * -1.0f;
            
            RedirectCycles = Config.Get("AutoRedirects", false);
            VertXSpacing = Config.Get("VertXSpacing", 64.0f);
            VertYSpacing = Config.Get("VertYSpacing", 16.0f);
            SaveOnExit = Config.Get("SaveLayoutOnExit", true);

            LoadBeforeOpen = Config.Get("LoadBeforeOpen", false);
        }
    }
}