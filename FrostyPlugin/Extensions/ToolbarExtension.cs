using SharpDX.Direct2D1;

namespace Frosty.Core
{
    /// <summary>
    /// Describes an extension to the toolbar. Classes that derive from this class can 
    /// specify the name, icon and action to perform when clicked.
    /// </summary>
    public abstract class ToolbarExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolbarExtension"/> class.
        /// </summary>
        public ToolbarExtension()
        {
        }
        
        /// <summary>
        /// When implemented in a derived class, gets the name of the toolbar item this extension will create.
        /// </summary>
        /// <returns>The name to use for the menu item.</returns>
        public virtual string Name { get; }


        /// <summary>
        /// When implemented in a derived class, gets the name of the toolbar item this extension will create.
        /// </summary>
        /// <returns>The name to use for the menu tooltip.</returns>
        public virtual string Tooltip { get; }

        /// <summary>
        /// When implemented in a derived class, gets the icon displayed for this toolbar item.
        /// </summary>
        /// <returns>The string that represents the icon location to display in the toolbar item.</returns>
        public virtual string Icon { get; }

        /// <summary>
        /// When implemented in a derived class, gets the action to perform when this too item is clicked.
        /// </summary>
        /// <returns>The action to perform when the toolbar item is clicked.</returns>
        public virtual RelayCommand ToolbarItemClicked { get; }
    }
}