using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of a node, or a vertex with a header, inputs, and outputs.
    /// </summary>
    public interface INode : IVertex
    {
        /// <summary>
        /// The input <see cref="IPort"/>s this node has
        /// </summary>
        ObservableCollection<IPort> Inputs { get; }
        
        /// <summary>
        /// The output <see cref="IPort"/>s this node has
        /// </summary>
        ObservableCollection<IPort> Outputs { get; }

        /// <summary>
        /// Occurs whenever a <see cref="IConnection"/> is created for an input <see cref="IPort"/>
        /// </summary>
        /// <param name="port">The port being updated</param>
        void OnInputUpdated(IPort port);
        
        /// <summary>
        /// Occurs whenever a <see cref="IConnection"/> is created for an output <see cref="IPort"/>
        /// </summary>
        /// <param name="port">The port being updated</param>
        void OnOutputUpdated(IPort port);

        /// <summary>
        /// Adds a <see cref="IPort"/> to the node
        /// </summary>
        /// <param name="port"></param>
        void AddPort(IPort port);

        /// <summary>
        /// Removes a <see cref="IPort"/> from the node
        /// </summary>
        /// <param name="port"></param>
        void RemovePort(IPort port);
    }
}