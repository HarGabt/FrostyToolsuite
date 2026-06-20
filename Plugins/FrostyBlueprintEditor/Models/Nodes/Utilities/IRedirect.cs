using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes.Utilities
{
    /// <summary>
    /// Base implementation for a transient which redirects a connection
    /// </summary>
    public interface IRedirect : INode, ITransient
    {
        /// <summary>
        /// Does this redirect have an Input or Output?
        /// </summary>
        PortDirection Direction { get; set; }
        
        /// <summary>
        /// If this Redirect is an Output(see <see cref="Direction"/>), this returns the source Redirect
        /// </summary>
        IRedirect SourceRedirect { get; set; }
        
        /// <summary>
        /// If this Redirect is an Input(see <see cref="Direction"/>), this returns the target Redirect
        /// </summary>
        IRedirect TargetRedirect { get; set; }
        
        /// <summary>
        /// The port being targeted by this redirect
        /// </summary>
        IPort RedirectTarget { get; set; }
    }
}