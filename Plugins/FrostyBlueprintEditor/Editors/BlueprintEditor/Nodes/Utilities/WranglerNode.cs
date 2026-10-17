using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using Frosty.Core;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    public class WranglerNode : BaseNode, ITransient, IRedirect
    {
        private const int FormatVersion = 100;

        public ConnectionType ConnectionType { get; set; }

        private IPort _originalSource;
        public IPort OriginalSource
        {
            get => _originalSource;
            set
            {
                _originalSource = value;
                UpdateDirection();
            }
        }

        private IPort _originalTarget;
        public IPort OriginalTarget
        {
            get => _originalTarget;
            set
            {
                _originalTarget = value;
                UpdateDirection();
            }
        }

        public Guid NodeId { get; set; } = Guid.NewGuid();

        public override string Header => "Wrangler";

        #region IRedirect

        public PortDirection Direction { get; set; }
        public IRedirect SourceRedirect { get; set; }
        public IRedirect TargetRedirect { get; set; }
        public IPort RedirectTarget { get; set; }

        #endregion

        private bool _loadedFromLayout;
        private bool _isFlipped;

        public bool IsFlipped
        {
            get => _isFlipped;
            set
            {
                if (_isFlipped != value)
                {
                    _isFlipped = value;
                    NotifyPropertyChanged(nameof(IsFlipped));
                }
            }
        }

        public void UpdateDirection()
        {
            IVertex sourceNode = OriginalSource?.Node as IVertex;
            IVertex targetNode = OriginalTarget?.Node as IVertex;
            if (sourceNode == null || targetNode == null)
                return;

            double sourceOutputX = sourceNode is WranglerNode
                ? sourceNode.Location.X + sourceNode.Size.Width / 2.0
                : sourceNode.Location.X + sourceNode.Size.Width;

            double targetInputX = targetNode is WranglerNode
                ? targetNode.Location.X + targetNode.Size.Width / 2.0
                : targetNode.Location.X;

            IsFlipped = sourceOutputX > targetInputX;
        }

        public WranglerNode(ConnectionType type, INodeWrangler wrangler) : base(wrangler)
        {
            ConnectionType = type;
            Size = new Size(30, 18);
            CreatePorts();
            PropertyChanged += OnPropertyChanged;
        }

        public WranglerNode(INodeWrangler wrangler) : base(wrangler)
        {
            PropertyChanged += OnPropertyChanged;
        }
        public WranglerNode() { }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location))
            {
                UpdateDirection();
                UpdateDownstreamDirections();
            }
        }

        private void UpdateDownstreamDirections()
        {
            if (Outputs.Count == 0 || NodeWrangler == null)
                return;

            foreach (IConnection conn in NodeWrangler.GetConnections(Outputs[0]))
            {
                if (conn.Source == Outputs[0] && conn.Target?.Node is WranglerNode downstream)
                {
                    downstream.UpdateDirection();
                }
            }
        }

        private void CreatePorts()
        {
            switch (ConnectionType)
            {
                case ConnectionType.Event:
                    Inputs.Add(new EventInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new EventOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Link:
                    Inputs.Add(new LinkInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new LinkOutput("Out", this) { Realm = Realm.Any });
                    break;
                case ConnectionType.Property:
                    Inputs.Add(new PropertyInput("In", this) { Realm = Realm.Any });
                    Outputs.Add(new PropertyOutput("Out", this) { Realm = Realm.Any });
                    break;
            }
        }

        public override void OnDestruction()
        {
            IsSelected = false;

            IConnection incoming = null;
            foreach (IConnection connection in NodeWrangler.GetConnections(Inputs[0]))
            {
                if (connection.Target == Inputs[0])
                {
                    incoming = connection;
                    break;
                }
            }

            List<IConnection> outgoing = new List<IConnection>();
            foreach (IConnection connection in NodeWrangler.GetConnections(Outputs[0]))
            {
                if (connection.Source == Outputs[0])
                    outgoing.Add(connection);
            }

            if (incoming != null && outgoing.Count > 0)
            {
                IPort bridgeSource = incoming.Source;
                foreach (IConnection conn in outgoing)
                    conn.Source = bridgeSource;

                NodeWrangler.RemoveConnection(incoming);
                return;
            }

            if (incoming != null)
            {
                NodeWrangler.RemoveConnection(incoming);
                return;
            }

            foreach (IConnection conn in outgoing)
                NodeWrangler.RemoveConnection(conn);
        }

        public override void OnCreation()
        {
            if (_loadedFromLayout)
            {
                _loadedFromLayout = false;

                if (OriginalSource != null && OriginalTarget != null && Inputs.Count > 0 && Outputs.Count > 0)
                {
                    // Find the EBX-backed connection from OriginalSource to OriginalTarget
                    IConnection existingConnection = null;
                    foreach (IConnection conn in NodeWrangler.Connections)
                    {
                        if (conn.Source == OriginalSource && conn.Target == OriginalTarget)
                        {
                            existingConnection = conn;
                            break;
                        }
                    }

                    if (existingConnection != null)
                    {
                        // Retarget it through the wrangler: OriginalSource -> wrangler.Inputs[0]
                        existingConnection.Target = Inputs[0];

                        // Add transient connection: wrangler.Outputs[0] -> OriginalTarget
                        NodeWrangler.AddConnection(new TransientConnection(Outputs[0], OriginalTarget, ConnectionType));
                    }
                }
            }

            UpdateDirection();
        }

        #region ITransient

        public bool Load(LayoutReader reader)
        {
            int first = reader.ReadInt();

            // Detect old format: first int was the ConnectionType (0-2)
            if (first >= 0 && first <= 2)
            {
                ConnectionType = (ConnectionType)first;
                Location = reader.ReadPoint();
                Size = new Size(30, 18);
                CreatePorts();
                return true;
            }

            // New format
            int version = first;
            NodeId = reader.ReadGuid();
            ConnectionType = (ConnectionType)reader.ReadInt();
            Location = reader.ReadPoint();
            Size = new Size(30, 18);
            CreatePorts();

            if (version >= FormatVersion)
            {
                OriginalSource = ReadPortRef(reader, PortDirection.Out);
                OriginalTarget = ReadPortRef(reader, PortDirection.In);
                _loadedFromLayout = true;
            }

            return true;
        }

        public void Save(LayoutWriter writer)
        {
            writer.Write(FormatVersion);
            writer.Write(NodeId);
            writer.Write((int)ConnectionType);
            writer.Write(Location);

            WritePortRef(writer, OriginalSource);
            WritePortRef(writer, OriginalTarget);
        }

        #endregion

        private void WritePortRef(LayoutWriter writer, IPort port)
        {
            if (port?.Node is IEntityNode entityNode)
            {
                writer.Write(true);
                writer.Write((byte)0);
                writer.Write(entityNode.InternalGuid);
                writer.WriteNullTerminatedString(port.Name);
                writer.Write((int)((EntityPort)port).Type);
            }
            else if (port?.Node is WranglerNode wranglerNode)
            {
                writer.Write(true);
                writer.Write((byte)1);
                writer.Write(wranglerNode.NodeId);
                writer.WriteNullTerminatedString(port.Name);
                writer.Write((int)((EntityPort)port).Type);
            }
            else
            {
                writer.Write(false);
            }
        }

        private IPort ReadPortRef(LayoutReader reader, PortDirection expectedDirection)
        {
            if (!reader.ReadBoolean())
                return null;

            byte nodeType = reader.ReadByte();

            if (nodeType == 0)
            {
                // IEntityNode: look up by InternalGuid
                AssetClassGuid guid = reader.ReadAssetClassGuid();
                string portName = reader.ReadNullTerminatedString();
                ConnectionType portType = (ConnectionType)reader.ReadInt();

                foreach (IVertex vertex in NodeWrangler.Vertices)
                {
                    if (!(vertex is IEntityNode entityNode))
                        continue;
                    if (!entityNode.InternalGuid.Equals(guid))
                        continue;

                    IPort port = expectedDirection == PortDirection.Out
                        ? entityNode.GetOutput(portName, portType)
                        : entityNode.GetInput(portName, portType);

                    if (port != null)
                        return port;

                    // Continue searching — multiple InterfaceNodes may share the same
                    // InternalGuid but differ in port name/type
                }
            }
            else if (nodeType == 1)
            {
                // WranglerNode: look up by NodeId
                Guid nodeId = reader.ReadGuid();
                string portName = reader.ReadNullTerminatedString();
                ConnectionType portType = (ConnectionType)reader.ReadInt();

                foreach (IVertex vertex in NodeWrangler.Vertices)
                {
                    if (!(vertex is WranglerNode wranglerNode))
                        continue;
                    if (wranglerNode.NodeId != nodeId)
                        continue;

                    var ports = expectedDirection == PortDirection.Out
                        ? wranglerNode.Outputs
                        : wranglerNode.Inputs;

                    foreach (IPort p in ports)
                    {
                        if (p.Name == portName && p is EntityPort ep && ep.Type == portType)
                            return p;
                    }
                }
            }

            return null;
        }
    }
}
