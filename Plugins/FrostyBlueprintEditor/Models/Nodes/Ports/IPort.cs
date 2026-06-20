using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Nodes.Utilities;

namespace BlueprintEditorPlugin.Models.Nodes.Ports
{
    public interface IPort : INotifyPropertyChanged
    {
        /// <summary>
        /// The name of the port
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// The direction this port faces on a node
        /// </summary>
        PortDirection Direction { get; }
        
        /// <summary>
        /// The position of this port on the graph
        /// </summary>
        Point Anchor { get; set; }
        
        /// <summary>
        /// The node this port belongs to. If this node is on a Redirect, it returns the targeted node. To get the redirect, use <see cref="RedirectNode"/> 
        /// </summary>
        INode Node { get; }
        
        /// <summary>
        /// If this port belongs to a Redirect, this returns the Redirect Node it belongs to. Otherwise null.
        /// </summary>
        IRedirect RedirectNode { get; set; }
        
        /// <summary>
        /// Whether or not this node has connections
        /// </summary>
        bool IsConnected { get; set; }
    }

    public enum PortDirection
    {
        In = 0,
        Out = 1
    }
}