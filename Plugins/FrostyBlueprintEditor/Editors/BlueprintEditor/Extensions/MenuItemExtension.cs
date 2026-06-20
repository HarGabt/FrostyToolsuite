using System.Windows.Media;
using BlueprintEditorPlugin.Editors.GraphEditor;
using Frosty.Core;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    /// <summary>
    /// Base class for a Menu Item Extension in the <see cref="BlueprintGraphEditor"/>
    /// </summary>
    public abstract class BlueprintMenuItemExtension
    {
        /// <summary>
        /// When implemented in a derived class, gets the name of the child menu item (of the top level menu item) this menu item will be placed
        /// </summary>
        /// <returns>The name of the child menu item to place the menu item into</returns>
        public virtual string SubLevelMenuName { get; }

        /// <summary>
        /// The name of this menu item to display in the UI
        /// </summary>
        public virtual string DisplayName { get; }
        
        /// <summary>
        /// When implemented in a derived class, this provides the ToolTip to display when hovered over
        /// </summary>
        public virtual string ToolTip { get; }
        
        /// <summary>
        /// When implemented in a derived class, this provides the Icon to display 
        /// </summary>
        public virtual ImageSource Icon { get; }
        
        public IEbxGraphEditor GraphEditor { get; internal set; }
        
        /// <summary>
        /// When implemented in a derived class, this series of actions will be performed after the button is clicked
        /// </summary>
        public virtual RelayCommand ButtonClicked { get; }

        /// <summary>
        /// Determines whether or not this extension is valid
        /// </summary>
        /// <returns>Whether this extension is valid</returns>
        public virtual bool IsValid()
        {
            return true;
        }
    }
}