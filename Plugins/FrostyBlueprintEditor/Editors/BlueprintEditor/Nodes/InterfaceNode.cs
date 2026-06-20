using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core.Controls;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    public class InterfaceNode : ITransient, IEntityNode
    {
        private string _header;
        private Point _location;
        private bool _isSelected;

        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                if (Direction == PortDirection.In)
                {
                    ((EntityNodeWrangler)NodeWrangler).UpdateInputInterfaceCache();
                }
                else
                {
                    ((EntityNodeWrangler)NodeWrangler).UpdateOutputInterfaceCache();
                }
                NotifyPropertyChanged(nameof(Header));
            }
        }

        /// <summary>
        /// The direction this node goes
        /// </summary>
        public PortDirection Direction => Inputs.Count != 0 ? PortDirection.In : PortDirection.Out;

        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
        }

        public Size Size { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        #region Object implementation

        public object Object { get; }
        
        /// <summary>
        /// The part of the interface <see cref="Object"/> which contains this nodes information
        /// </summary>
        public object SubObject { get; set; }
        
        /// <summary>
        /// blehhh
        /// </summary>
        public EditInterfaceArgs EditArgs { get; set; }

        public PointerRefType Type => PointerRefType.Internal;
        public AssetClassGuid InternalGuid { get; set; }
        
        public Guid FileGuid { get; }
        public Guid ClassGuid { get; }

        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            if (args.OldValue == args.NewValue)
                return;

            switch (args.Item.Name)
            {
                case "Name":
                {
                    EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
                    if (wrangler.GetInterfaceNode(EditArgs.Name, Direction, ConnectionType) != null)
                    {
                        App.Logger.LogError("Cannot have 2 interface nodes of the same name.");
                        EditArgs.Name = args.OldValue.ToString();
                        return;
                    }
                    
                    ((dynamic)SubObject).Name = new CString(EditArgs.Name);
                    
                    Header = EditArgs.Name;
                    if (Direction == PortDirection.In)
                    {
                        Inputs[0].Name = Header;
                
                        if (ConnectionType == ConnectionType.Property)
                        {
                            if (((dynamic)SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                            {
                                InterfaceNode interfaceNode = wrangler.GetInterfaceNode(args.OldValue.ToString(), PortDirection.Out, ConnectionType);
                                interfaceNode.Header = Header;
                                interfaceNode.Outputs[0].Name = Header;
                            }
                        }
                    }
                    else
                    {
                        Outputs[0].Name = Header;
                
                        if (ConnectionType == ConnectionType.Property)
                        {
                            if (((dynamic)SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                            {
                                InterfaceNode interfaceNode = wrangler.GetInterfaceNode(args.OldValue.ToString(), PortDirection.In, ConnectionType);
                                interfaceNode.Header = Header;
                                interfaceNode.Inputs[0].Name = Header;
                            }
                        }
                    }
                } break;
                case "Value":
                {
                    if (ConnectionType == ConnectionType.Property)
                    {
                        ((dynamic)SubObject).Value = new CString(((EditPropInterfaceArgs)EditArgs).Value);
                    }
                } break;
            }
        }

        public EntityInput GetInput(string name, ConnectionType type)
        {
            return (EntityInput)Inputs[0];
        }

        public EntityOutput GetOutput(string name, ConnectionType type)
        {
            return (EntityOutput)Outputs[0];
        }

        public void AddInput(EntityInput input)
        {
            throw new NotImplementedException();
        }

        public void AddOutput(EntityOutput output)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This will safely try to set a property on the node's <see cref="Object"/>
        /// </summary>
        /// <param name="name">Name of the property to set</param>
        /// <param name="value">Value of the property</param>
        /// <returns>Whether or not the property was set</returns>
        public bool TrySetProperty(string name, object value)
        {
            if (Object == null)
                return false;
            
            PropertyInfo property = Object.GetType().GetProperty(name);
            if (property != null)
            {
                try
                {
                    property.SetValue(Object, value);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a value from the node's <see cref="Object"/>
        /// </summary>
        /// <param name="name">The name of the property to fetch</param>
        /// <returns>The value of the property. Null if it was not found</returns>
        public object TryGetProperty(string name)
        {
            if (Object == null)
                return null;
            
            PropertyInfo property = Object.GetType().GetProperty(name);
            if (property != null)
            {
                return property.GetValue(Object);
            }

            return null;
        }

        public ConnectionType ConnectionType { get; set; }

        #endregion

        #region Property Changing

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Basic implementation

        public INodeWrangler NodeWrangler { get; }

        public bool IsValid()
        {
            return true;
        }

        public void OnCreation()
        {
        }

        public void OnDestruction()
        {
        }

        #endregion

        #region Complex implementation

        public void OnInputUpdated(IPort port)
        {
        }

        public void OnOutputUpdated(IPort port)
        {
        }

        public void AddPort(IPort port)
        {
            Inputs.Add(port);
        }

        public void RemovePort(IPort port)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Interface implementation
        
        public bool Load(LayoutReader reader)
        {
            string name = reader.ReadNullTerminatedString();
            ConnectionType = (ConnectionType)reader.ReadInt();

            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            InterfaceNode real;
            if (reader.ReadBoolean()) // Is an output
            {
                real = wrangler.GetInterfaceNode(name, PortDirection.Out, ConnectionType);
            }
            else
            {
                real = wrangler.GetInterfaceNode(name, PortDirection.In, ConnectionType);
            }

            if (real == null)
            {
                reader.ReadPoint();
                return false;
            }

            real.Location = reader.ReadPoint();
            
            // TODO HACK: We create interface nodes before we read layouts. So in order to read transient data from them, we can't add new ones.
            // Resulting in this hack of a method
            return false;
        }

        public void Save(LayoutWriter writer)
        {
            writer.WriteNullTerminatedString(Header);
            writer.Write((int)ConnectionType);
            writer.Write(Inputs.Count == 0);
            writer.Write(Location);
        }

        #endregion

        #region Networking implementation

        public Realm Realm
        {
            get
            {
                if (Inputs.Count != 0)
                {
                    return ((EntityPort)Inputs[0]).Realm;
                }
                else
                {
                    return ((EntityPort)Outputs[0]).Realm;
                }
            }
        }

        #endregion

        public InterfaceNode(object obj, object subObject, string name, ConnectionType type, PortDirection direction, INodeWrangler wrangler)
        {
            Object = obj;
            SubObject = subObject;
            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            if (name.StartsWith("0x"))
            {
                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                _header = Utils.GetString(hash);
                NotifyPropertyChanged(nameof(Header));
            }
            else
            {
                _header = name;
                NotifyPropertyChanged(nameof(Header));
            }
            ConnectionType = type;
            NodeWrangler = wrangler;
            
            if (direction == PortDirection.In)
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        EventInput input = new EventInput(Header, this)
                        {
                            Realm = Realm.Any
                        };
                        Inputs.Add(input);
                    } break;
                    case ConnectionType.Link:
                    {
                        LinkInput input = new LinkInput(Header, this)
                        {
                            Realm = Realm.Any
                        };
                        Inputs.Add(input);
                    } break;
                    case ConnectionType.Property:
                    {
                        PropertyInput input = new PropertyInput(Header, this)
                        {
                            Realm = Realm.Any,
                            IsInterface = true
                        };
                        Inputs.Add(input);
                    } break;
                }
            }
            else
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        EventOutput input = new EventOutput(Header, this)
                        {
                            Realm = Realm.Any
                        };
                        Outputs.Add(input);
                    } break;
                    case ConnectionType.Link:
                    {
                        LinkOutput input = new LinkOutput(Header, this)
                        {
                            Realm = Realm.Any
                        };
                        Outputs.Add(input);
                    } break;
                    case ConnectionType.Property:
                    {
                        PropertyOutput input = new PropertyOutput(Header, this)
                        {
                            Realm = Realm.Any,
                            IsInterface = true
                        };
                        Outputs.Add(input);
                    } break;
                }
            }

            if (type == ConnectionType.Property)
            {
                EditArgs = new EditPropInterfaceArgs(this);
            }
            else
            {
                EditArgs = new EditInterfaceArgs(this);
            }
        }

        public InterfaceNode(INodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }

        public InterfaceNode()
        {
        }

        public override string ToString()
        {
            return $"{ConnectionType} Interface {Header}";
        }
    }

    public class EditInterfaceArgs
    {
        [Description("The name of this interface")]
        public string Name { get; set; }

        public InterfaceNode Node;

        public EditInterfaceArgs(InterfaceNode node)
        {
            Name = node.Header;
            Node = node;
        }

        public EditInterfaceArgs()
        {
            Name = "";
        }
    }

    public class EditPropInterfaceArgs : EditInterfaceArgs
    {
        [Description("The value of this interface")]
        public string Value { get; set; }

        public EditPropInterfaceArgs(InterfaceNode node)
        {
            Name = node.Header;
            Node = node;
            Value = ((dynamic)node.SubObject).Value.ToString() ?? "";
        }
        
        public EditPropInterfaceArgs()
        {
            Value = "";
        }
    }

    public class AddInterfaceArgs
    {
        [Description("The name of this interface")]
        public string Name { get; set; }
        
        [Description("The direction of this interface. Out means it will have 1 output, and when referenced externally will be an input. In is the inverse")]
        public PortDirection Direction { get; set; }
        
        [Description("The type of connection that can plug into this interface")]
        public ConnectionType Type { get; set; }

        public AddInterfaceArgs()
        {
            Name = "";
        }
    }
}