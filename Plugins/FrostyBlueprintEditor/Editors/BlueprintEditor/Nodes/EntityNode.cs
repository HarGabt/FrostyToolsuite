using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    /// <summary>
    /// A basic implementation of an entity in a node form. For creation, please see <see cref="GetNodeFromEntity(object,BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler.INodeWrangler,bool)"/> or its overloads
    /// </summary>
    public class EntityNode : IEntityNode, INetworked
    {
        #region Generic node implementation

        #region Text Data

        private string _header;
        
        /// <summary>
        /// The text displayed at the top of the node. By default set as the Types name.
        /// </summary>
        public virtual string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }
        
        private string _footer;
        
        /// <summary>
        /// Smaller subtext displayed below the <see cref="Header"/>
        /// </summary>
        public virtual string Footer
        {
            get => _footer;
            set
            {
                _footer = value;
                NotifyPropertyChanged(nameof(Footer));
            }
        }

        private string _toolTip;
        
        /// <summary>
        /// The tooltip displayed when hovering over the node. Also used for the docbox
        /// </summary>
        public virtual string ToolTip
        {
            get => _toolTip;
            set
            {
                _toolTip = value;
                NotifyPropertyChanged(nameof(ToolTip));
            }
        }

        #endregion

        #region Positional data

        public Size Size { get; set; }

        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _isFlatted;
        public bool IsFlatted
        {
            get => _isFlatted;
            set
            {
                _isFlatted = value;
                NotifyPropertyChanged(nameof(IsFlatted));
            }
        }
        
        private Point _location;
        public Point Location
        {
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
            get => _location;
        }

        #endregion

        #region Node data

        public ObservableCollection<IPort> Inputs { get; protected set; }
        public ObservableCollection<IPort> Outputs { get; protected set; }

        public INodeWrangler NodeWrangler { get; private set; }

        #endregion

        #endregion
        
        #region Commands

        public ICommand CopyCommand => new DelegateCommand(Copy);

        public void Copy()
        {
            FrostyClipboard.Current.SetData(Object);
        }
        
        public ICommand RedirectInCommand => new DelegateCommand(RedirectIn);

        private void RedirectIn()
        {
            FrostyTaskWindow.Show("Redirecting...", "", task =>
            {
                foreach (IPort input in Inputs)
                {
                    if (input is EntityInput entityInput)
                    {
                        entityInput.Redirect();
                    }
                }
            });
        }
        
        public ICommand RedirectOutCommand => new DelegateCommand(RedirectOut);

        private void RedirectOut()
        {
            FrostyTaskWindow.Show("Redirecting...", "", task =>
            {
                foreach (IPort output in Outputs)
                {
                    if (output is EntityOutput entityOutput)
                    {
                        entityOutput.Redirect();
                    }
                }
            });
        }
        
        public ICommand AddPortCommand => new DelegateCommand(UserAddPort);

        private protected void UserAddPort()
        {
            AddPortArgs args = new AddPortArgs();
            MessageBoxResult result = EditPromptWindow.Show(args, "Set new Properties");
            if (result == MessageBoxResult.Yes)
            {
                if (args.Direction == PortDirection.In)
                {
                    switch (args.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            AddPort(new EventInput(args.Name, this)
                            {
                                HasPlayer = args.HasPlayer,
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Link:
                        {
                            AddPort(new LinkInput(args.Name, this)
                            {
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Property:
                        {
                            AddPort(new PropertyInput(args.Name, this)
                            {
                                IsInterface = args.IsInterface,
                                Realm = args.Realm
                            });
                        } break;
                    }
                }
                else
                {
                    switch (args.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            AddPort(new EventOutput(args.Name, this)
                            {
                                HasPlayer = args.HasPlayer,
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Link:
                        {
                            AddPort(new LinkOutput(args.Name, this)
                            {
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Property:
                        {
                            AddPort(new PropertyOutput(args.Name, this)
                            {
                                IsInterface = args.IsInterface,
                                Realm = args.Realm
                            });
                        } break;
                    }
                }
            }
        }

        #endregion

        #region Entity Info

        private Realm _realm;
        public virtual Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                NotifyPropertyChanged(nameof(Realm));
            }
        }

        public object Object { get; private set; }
        public virtual string ObjectType { get; private set; }

        private bool _hasPlayerEvent;
        public bool HasPlayerEvent
        {
            get => _hasPlayerEvent;
            set
            {
                _hasPlayerEvent = value;
                NotifyPropertyChanged(nameof(HasPlayerEvent));
            }
        }

        #region Object Location

        public PointerRefType Type { get; private set; }
        
        // Internal
        public AssetClassGuid InternalGuid => ((dynamic)Object).GetInstanceGuid();

        // External
        public Guid FileGuid { get; private set; }
        public Guid ClassGuid { get; private set; }

        #endregion

        #region Networked implementation

        public Realm ParseRealm(object obj)
        {
            switch (obj.ToString())
            {
                case "Realm_Server":
                {
                    return Realm.Server;
                }
                case "Realm_Client":
                {
                    return Realm.Client;
                }
                case "Realm_ClientAndServer":
                {
                    return Realm.ClientAndServer;
                }
                default:
                {
                    return Realm.Invalid;
                }
            }
        }

        
        public virtual Realm DetermineRealm(bool ignoreCurrent = false)
        {
            Realm realm = Realm;

            if ((Realm == Realm.Any || Realm == Realm.Invalid) || ignoreCurrent)
            {
                foreach (EntityConnection connection in NodeWrangler.GetConnections(this))
                {
                    if (connection.Realm != Realm.Any && connection.Realm != Realm.Invalid)
                    {
                        realm = connection.DetermineRealm();
                        return realm;
                    }
                }
            }

            return realm;
        }

        public void FixRealm()
        {
            Realm = DetermineRealm();
        }
        
        public void ForceFixRealm()
        {
            Realm = DetermineRealm(true);
        }

        #endregion

        /// <summary>
        /// This method is called whenever the <see cref="Footer"/> needs to be updated. For example when the object is modified
        /// </summary>
        public virtual void BuildFooter()
        {
            // TODO: Automatically build footer based on properties
        }

        #endregion

        #region Basic node implementation

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public virtual bool IsValid()
        {
            return true;
        }

        public virtual void OnCreation()
        {
            if (Header == null)
            {
                Header = ObjectType;
            }
            
            if (TryGetProperty("Realm") != null)
            {
                Realm = ParseRealm(TryGetProperty("Realm").ToString());
            }
            else
            {
                Realm = Realm.Any;
            }
            
            // Update our input/output realms 
            foreach (EntityInput input in Inputs)
            {
                if (input.Realm == Realm.Invalid)
                {
                    input.Realm = Realm;
                }
            }
                
            foreach (EntityOutput output in Outputs)
            {
                if (output.Realm == Realm.Invalid)
                {
                    output.Realm = Realm;
                }
            }
            
            try
            {
                BuildFooter();
            }
            catch (Exception e)
            {
                ClearFooter();
            }
        }

        public virtual void OnDestruction()
        {
        }

        public virtual void OnInputUpdated(IPort port)
        {
            EntityPort entityPort = (EntityPort)port;
            entityPort.FixRealm();

            object value = TryGetProperty("Flags");
            if (value == null)
                return;
            
            if (entityPort.Type == ConnectionType.Link)
                return;

            ObjectFlagsHelper flagsHelper = new ObjectFlagsHelper((uint)value);
            
            switch (entityPort.Realm)
            {
                case Realm.NetworkedClientAndServer when entityPort.Type == ConnectionType.Event:
                case Realm.ClientAndServer when entityPort.Type == ConnectionType.Event:
                {
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ClientEvent = true;
                    }
                    else
                    {
                        flagsHelper.ClientEvent = false;
                    }
                    
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ServerEvent = true;
                    }
                    else
                    {
                        flagsHelper.ServerEvent = false;
                    }
                } break;
                case Realm.NetworkedClient when entityPort.Type == ConnectionType.Event:
                case Realm.Client when entityPort.Type == ConnectionType.Event:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ClientEvent = true;
                    }
                    else
                    {
                        flagsHelper.ClientEvent = false;
                    }
                } break;
                case Realm.Server when entityPort.Type == ConnectionType.Event:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ServerEvent = true;
                    }
                    else
                    {
                        flagsHelper.ServerEvent = false;
                    }
                } break;
                
                case Realm.NetworkedClientAndServer when entityPort.Type == ConnectionType.Property:
                case Realm.ClientAndServer when entityPort.Type == ConnectionType.Property:
                {
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ClientProperty = true;
                    }
                    else
                    {
                        flagsHelper.ClientProperty = false;
                    }
                    
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ServerProperty = true;
                    }
                    else
                    {
                        flagsHelper.ServerProperty = false;
                    }
                } break;
                case Realm.NetworkedClient when entityPort.Type == ConnectionType.Property:
                case Realm.Client when entityPort.Type == ConnectionType.Property:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ClientProperty = true;
                    }
                    else
                    {
                        flagsHelper.ClientProperty = false;
                    }
                } break;
                case Realm.Server when entityPort.Type == ConnectionType.Property:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ServerProperty = true;
                    }
                    else
                    {
                        flagsHelper.ServerProperty = false;
                    }
                } break;
                
                case Realm.Any:
                {
                    bool client = false;
                    bool server = false;
                    foreach (IConnection connection in NodeWrangler.GetConnections(port))
                    {
                        if (connection is EntityConnection entityConnection)
                        {
                            switch (entityConnection.DetermineRealm())
                            {
                                case Realm.Any:
                                    continue;
                                case Realm.Invalid:
                                    continue;
                                case Realm.NetworkedClientAndServer:
                                case Realm.ClientAndServer:
                                {
                                    client = true;
                                    server = true;
                                } break;
                                case Realm.NetworkedClient:
                                case Realm.Client:
                                {
                                    client = true;
                                } break;
                                case Realm.Server:
                                {
                                    server = true;
                                } break;
                            }
                        }
                        
                        if (client && server)
                            break;
                    }

                    if (client)
                    {
                        switch (entityPort.Type)
                        {
                            case ConnectionType.Event:
                            {
                                flagsHelper.ClientEvent = true;
                            } break;
                            case ConnectionType.Property:
                            {
                                flagsHelper.ClientProperty = true;
                            } break;
                        }
                    }

                    if (server)
                    {
                        switch (entityPort.Type)
                        {
                            case ConnectionType.Event:
                            {
                                flagsHelper.ServerEvent = true;
                            } break;
                            case ConnectionType.Property:
                            {
                                flagsHelper.ServerProperty = true;
                            } break;
                        }
                    }
                } break;
            }
            
            TrySetProperty("Flags", flagsHelper.GetAsFlags());
        }

        public virtual void OnOutputUpdated(IPort port)
        {
            EntityPort entityPort = (EntityPort)port;
            entityPort.FixRealm();
            
            if (entityPort.Type != ConnectionType.Link)
                return;
            
            object value = TryGetProperty("Flags");
            if (value == null)
                return;
            
            ObjectFlagsHelper flagsHelper = new ObjectFlagsHelper((uint)value);
            
            switch (entityPort.Realm)
            {
                case Realm.NetworkedClientAndServer:
                case Realm.ClientAndServer:
                {
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ClientLinkSource = true;
                    }
                    else
                    {
                        flagsHelper.ClientLinkSource = false;
                    }
                    
                    if (NodeWrangler.GetConnections(port).Any(c =>
                            c is EntityConnection e && (e.Realm == Realm.Server || e.Realm == Realm.ClientAndServer)))
                    {
                        flagsHelper.ServerLinkSource = true;
                    }
                    else
                    {
                        flagsHelper.ServerLinkSource = false;
                    }
                } break;
                case Realm.NetworkedClient:
                case Realm.Client:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ClientLinkSource = true;
                    }
                    else
                    {
                        flagsHelper.ClientLinkSource = false;
                    }
                } break;
                case Realm.Server:
                {
                    if (NodeWrangler.GetConnections(port).Any())
                    {
                        flagsHelper.ServerLinkSource = true;
                    }
                    else
                    {
                        flagsHelper.ServerLinkSource = false;
                    }
                } break;
            }

            TrySetProperty("Flags", flagsHelper.GetAsFlags());
        }
        
        public virtual void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            if (args.Item.Name == "Realm")
            {
                Realm = ParseRealm(args.NewValue);
                Realm oldRealm = ParseRealm(args.OldValue);
                foreach (EntityPort input in Inputs)
                {
                    if (input.Realm == oldRealm)
                    {
                        input.Realm = Realm;
                    }
                }
                foreach (EntityPort output in Outputs)
                {
                    if (output.Realm == oldRealm)
                    {
                        output.Realm = Realm;
                    }
                }

                foreach (IConnection connection in NodeWrangler.GetConnections(this))
                {
                    if (connection is EntityConnection entityConnection)
                    {
                        entityConnection.ForceFixRealm();
                    }
                }
            }

            try
            {
                BuildFooter();
            }
            catch (Exception e)
            {
                ClearFooter();
            }
        }

        #endregion

        #region Complex node implementation

        private Dictionary<int, PropertyInput> _hashCachePInputs = new Dictionary<int, PropertyInput>();
        private Dictionary<int, LinkInput> _hashCacheLInputs = new Dictionary<int, LinkInput>();
        private Dictionary<int, EventInput> _hashCacheEInputs = new Dictionary<int, EventInput>();
        
        private Dictionary<int, PropertyOutput> _hashCachePOutputs = new Dictionary<int, PropertyOutput>();
        private Dictionary<int, LinkOutput> _hashCacheLOutputs = new Dictionary<int, LinkOutput>();
        private Dictionary<int, EventOutput> _hashCacheEOutputs = new Dictionary<int, EventOutput>();

        #region Caching

        /// <summary>
        /// Remakes the port cache based on <see cref="Inputs"/> and <see cref="Outputs"/>
        /// </summary>
        public void RefreshCache()
        {
            _hashCachePInputs.Clear();
            _hashCacheLInputs.Clear();
            _hashCacheEInputs.Clear();
            
            _hashCachePOutputs.Clear();
            _hashCacheLOutputs.Clear();
            _hashCacheEOutputs.Clear();

            List<IPort> inputs = Inputs.ToList();
            foreach (IPort port in inputs)
            {
                if (!(port is EntityInput))
                {
                    App.Logger.LogError("Port {0} on {1} is not a EntityInput, despite not being added to inputs.", port.Name, ToString());
                    Inputs.Remove(port);
                }
                
                var input = (EntityInput)port;
                switch (input.Type)
                {
                    case ConnectionType.Event:
                    {
                        if (input.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCacheEInputs.Add(hash, (EventInput)input);
                        }
                        else
                        {
                            int hash = Utils.HashString(input.Name);
                            _hashCacheEInputs.Add(hash, (EventInput)input);
                        }
                    } break;
                    case ConnectionType.Link:
                    {
                        if (input.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCacheLInputs.Add(hash, (LinkInput)input);
                        }
                        else
                        {
                            int hash = Utils.HashString(input.Name);
                            _hashCacheLInputs.Add(hash, (LinkInput)input);
                        }
                    } break;
                    case ConnectionType.Property:
                    {
                        if (input.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCachePInputs.Add(hash, (PropertyInput)input);
                        }
                        else
                        {
                            int hash = Utils.HashString(input.Name);
                            _hashCachePInputs.Add(hash, (PropertyInput)input);
                        }
                    } break;
                }
            }

            List<IPort> outputs = Outputs.ToList();
            foreach (IPort port in outputs)
            {
                if (!(port is EntityOutput))
                {
                    App.Logger.LogError("Port {0} on {1} is not a EntityOutput, despite not being added to inputs.", port.Name, ToString());
                    Outputs.Remove(port);
                }
                
                var output = (EntityOutput)port;
                switch (output.Type)
                {
                    case ConnectionType.Event:
                    {
                        if (output.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCacheEOutputs.Add(hash, (EventOutput)output);
                        }
                        else
                        {
                            int hash = Utils.HashString(output.Name);
                            _hashCacheEOutputs.Add(hash, (EventOutput)output);
                        }
                    } break;
                    case ConnectionType.Link:
                    {
                        if (output.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCacheLOutputs.Add(hash, (LinkOutput)output);
                        }
                        else
                        {
                            int hash = Utils.HashString(output.Name);
                            _hashCacheLOutputs.Add(hash, (LinkOutput)output);
                        }
                    } break;
                    case ConnectionType.Property:
                    {
                        if (output.Name.StartsWith("0x"))
                        {
                            int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                            _hashCachePOutputs.Add(hash, (PropertyOutput)output);
                        }
                        else
                        {
                            int hash = Utils.HashString(output.Name);
                            _hashCachePOutputs.Add(hash, (PropertyOutput)output);
                        }
                    } break;
                }
            }
        }

        #endregion

        #region Port retrieval

        public EntityInput GetInput(string name, ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Event:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetInput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCacheEInputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCacheEInputs[Utils.HashString(name)];
                }
                case ConnectionType.Link:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetInput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCacheLInputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCacheLInputs[Utils.HashString(name)];
                }
                case ConnectionType.Property:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetInput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCachePInputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCachePInputs[Utils.HashString(name)];
                }
            }

            return null;
        }
        public EntityInput GetInput(int hash, ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Event:
                {
                    if (!_hashCacheEInputs.ContainsKey(hash))
                        return null;

                    return _hashCacheEInputs[hash];
                }
                case ConnectionType.Link:
                {
                    if (!_hashCacheLInputs.ContainsKey(hash))
                        return null;

                    return _hashCacheLInputs[hash];
                }
                case ConnectionType.Property:
                {
                    if (!_hashCachePInputs.ContainsKey(hash))
                        return null;

                    return _hashCachePInputs[hash];
                }
            }

            return null;
        }
        
        public EntityOutput GetOutput(string name, ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Event:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetOutput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCacheEOutputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCacheEOutputs[Utils.HashString(name)];
                }
                case ConnectionType.Link:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetOutput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCacheLOutputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCacheLOutputs[Utils.HashString(name)];
                }
                case ConnectionType.Property:
                {
                    if (name.StartsWith("0x"))
                    {
                        return GetOutput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier), type);
                    }
            
                    if (!_hashCachePOutputs.ContainsKey(Utils.HashString(name)))
                        return null;

                    return _hashCachePOutputs[Utils.HashString(name)];
                }
            }

            return null;
        }

        public EntityOutput GetOutput(int hash, ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Event:
                {
                    if (!_hashCacheEOutputs.ContainsKey(hash))
                        return null;

                    return _hashCacheEOutputs[hash];
                }
                case ConnectionType.Link:
                {
                    if (!_hashCacheLOutputs.ContainsKey(hash))
                        return null;

                    return _hashCacheLOutputs[hash];
                }
                case ConnectionType.Property:
                {
                    if (!_hashCachePOutputs.ContainsKey(hash))
                        return null;

                    return _hashCachePOutputs[hash];
                }
            }

            return null;
        }

        #endregion

        #region Port adding
        
        public void AddPort(IPort port)
        {
            if (port is EntityOutput output)
            {
                AddOutput(output);
            }
            else
            {
                AddInput((EntityInput)port); // Has to be an entity input
            }
        }

        public void AddInput(EntityInput input)
        {
            if (input.Name.StartsWith("0x"))
            {
                switch (input.Type)
                {
                    case ConnectionType.Event:
                    {
                        int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCacheEInputs.ContainsKey(hash))
                            return;
                
                        Inputs.Add(input);
                        _hashCacheEInputs.Add(hash, (EventInput)input);
                        return;
                    }
                    case ConnectionType.Link:
                    {
                        int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCacheLInputs.ContainsKey(hash))
                            return;
                
                        Inputs.Add(input);
                        _hashCacheLInputs.Add(hash, (LinkInput)input);
                        return;
                    }
                    case ConnectionType.Property:
                    {
                        int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCachePInputs.ContainsKey(hash))
                            return;
                
                        Inputs.Add(input);
                        _hashCachePInputs.Add(hash, (PropertyInput)input);
                        return;
                    }
                }
            }

            switch (input.Type)
            {
                case ConnectionType.Event:
                {
                    if (_hashCacheEInputs.ContainsKey(Utils.HashString(input.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && input.Realm == Realm.Invalid)
                    {
                        input.Realm = Realm;
                    }
            
                    Inputs.Add(input);
                    _hashCacheEInputs.Add(Utils.HashString(input.Name), (EventInput)input);
                } break;
                case ConnectionType.Link:
                {
                    if (_hashCacheLInputs.ContainsKey(Utils.HashString(input.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && input.Realm == Realm.Invalid)
                    {
                        input.Realm = Realm;
                    }
            
                    Inputs.Add(input);
                    _hashCacheLInputs.Add(Utils.HashString(input.Name), (LinkInput)input);
                } break;
                case ConnectionType.Property:
                {
                    if (_hashCachePInputs.ContainsKey(Utils.HashString(input.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && input.Realm == Realm.Invalid)
                    {
                        input.Realm = Realm;
                    }
            
                    Inputs.Add(input);
                    _hashCachePInputs.Add(Utils.HashString(input.Name), (PropertyInput)input);
                } break;
            }
        }

        public EntityInput AddInput(string name, ConnectionType type, Realm realm = Realm.Any, bool isInterface = false)
        {
            EntityInput input = null;
            
            switch (type)
            {
                case ConnectionType.Event:
                {
                    input = new EventInput(name, this)
                    {
                        Realm = realm
                    };
                    AddInput(input);
                } break;
                case ConnectionType.Link:
                {
                    input = new LinkInput(name, this)
                    {
                        Realm = realm
                    };
                    AddInput(input);
                } break;
                case ConnectionType.Property:
                {
                    input = new PropertyInput(name, this)
                    {
                        Realm = realm,
                        IsInterface = isInterface
                    };
                    AddInput(input);
                } break;
            }

            return input;
        }

        public void AddOutput(EntityOutput output)
        {
            if (output.Name.StartsWith("0x"))
            {
                switch (output.Type)
                {
                    case ConnectionType.Event:
                    {
                        int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCacheEOutputs.ContainsKey(hash))
                            return;
                
                        Outputs.Add(output);
                        _hashCacheEOutputs.Add(hash, (EventOutput)output);
                        return;
                    }
                    case ConnectionType.Link:
                    {
                        int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCacheLOutputs.ContainsKey(hash))
                            return;
                
                        Outputs.Add(output);
                        _hashCacheLOutputs.Add(hash, (LinkOutput)output);
                        return;
                    }
                    case ConnectionType.Property:
                    {
                        int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        if (_hashCachePOutputs.ContainsKey(hash))
                            return;
                
                        Outputs.Add(output);
                        _hashCachePOutputs.Add(hash, (PropertyOutput)output);
                        return;
                    }
                }
            }

            switch (output.Type)
            {
                case ConnectionType.Event:
                {
                    if (_hashCacheEOutputs.ContainsKey(Utils.HashString(output.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && output.Realm == Realm.Invalid)
                    {
                        output.Realm = Realm;
                    }
            
                    Outputs.Add(output);
                    _hashCacheEOutputs.Add(Utils.HashString(output.Name), (EventOutput)output);
                } break;
                case ConnectionType.Link:
                {
                    if (_hashCacheLOutputs.ContainsKey(Utils.HashString(output.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && output.Realm == Realm.Invalid)
                    {
                        output.Realm = Realm;
                    }
            
                    Outputs.Add(output);
                    _hashCacheLOutputs.Add(Utils.HashString(output.Name), (LinkOutput)output);
                } break;
                case ConnectionType.Property:
                {
                    if (_hashCachePOutputs.ContainsKey(Utils.HashString(output.Name)))
                        return;

                    if (TryGetProperty("Realm") != null && output.Realm == Realm.Invalid)
                    {
                        output.Realm = Realm;
                    }
            
                    Outputs.Add(output);
                    _hashCachePOutputs.Add(Utils.HashString(output.Name), (PropertyOutput)output);
                } break;
            }
        }
        
        public EntityOutput AddOutput(string name, ConnectionType type, Realm realm = Realm.Any, bool hasPlayer = false)
        {
            EntityOutput output = null;
            switch (type)
            {
                case ConnectionType.Event:
                {
                    output = new EventOutput(name, this)
                    {
                        Realm = realm,
                        HasPlayer = hasPlayer
                    };
                    
                    AddOutput(output);
                } break;
                case ConnectionType.Link:
                {
                    output = new LinkOutput(name, this)
                    {
                        Realm = realm
                    };
                    AddOutput(output);
                } break;
                case ConnectionType.Property:
                {
                    output = new PropertyOutput(name, this)
                    {
                        Realm = realm
                    };
                    AddOutput(output);
                } break;
            }

            return output;
        }

        #endregion

        #region Port Removing
        
        public void RemovePort(IPort port)
        {
            if (port.Direction == PortDirection.In)
            {
                RemoveInput((EntityInput)port);
            }
            else
            {
                RemoveOutput((EntityOutput)port);
            }
        }

        public void RemoveInput(string name, ConnectionType type)
        {
            EntityInput input = GetInput(name, type);
            if (input == null)
                return;
            
            RemoveInput(input);
        }
        
        public void RemoveInput(EntityInput input)
        {
            NodeWrangler.ClearConnections(input);
            
            switch (input.Type)
            {
                case ConnectionType.Event:
                {
                    _hashCacheEInputs.Remove(Utils.HashString(input.Name));
                } break;
                case ConnectionType.Link:
                {
                    _hashCacheLInputs.Remove(Utils.HashString(input.Name));
                } break;
                case ConnectionType.Property:
                {
                    _hashCachePInputs.Remove(Utils.HashString(input.Name));
                } break;
            }
            
            Inputs.Remove(input);
        }
        
        public void RemoveOutput(string name, ConnectionType type)
        {
            EntityOutput output = GetOutput(name, type);
            if (output == null)
                return;
            
            RemoveOutput(output);
        }
        
        public void RemoveOutput(EntityOutput output)
        {
            NodeWrangler.ClearConnections(output);

            switch (output.Type)
            {
                case ConnectionType.Event:
                {
                    _hashCacheEOutputs.Remove(Utils.HashString(output.Name));
                } break;
                case ConnectionType.Link:
                {
                    _hashCacheLOutputs.Remove(Utils.HashString(output.Name));
                } break;
                case ConnectionType.Property:
                {
                    _hashCachePOutputs.Remove(Utils.HashString(output.Name));
                } break;
            }
            
            Outputs.Remove(output);
        }

        #endregion

        #region Port Clearing

        public void ClearInputs()
        {
            NodeWrangler.ClearConnections(this, PortDirection.In);
            _hashCacheEInputs.Clear();
            _hashCacheLInputs.Clear();
            _hashCachePInputs.Clear();
            Inputs.Clear();
        }
        
        public void ClearOutputs()
        {
            NodeWrangler.ClearConnections(this, PortDirection.Out);
            _hashCachePOutputs.Clear();
            _hashCacheLOutputs.Clear();
            _hashCacheEOutputs.Clear();
            Outputs.Clear();
        }

        #endregion

        #region Footer Text

        public void AddFooter(string value)
        {
            if (Footer != null)
            {
                Footer += $"\n{value}";
            }
            else
            {
                Footer = value;
            }
        }

        public void RemoveFooter(string value)
        {
            if (Footer == null)
                return;
            
            // TODO: Work around, we need to account for value potentially being on a new line
            Footer = Footer.Replace($"\n{value}", "");
            Footer = Footer.Replace(value, "");
        }

        public void ClearFooter()
        {
            Footer = null;
        }

        #endregion

        #endregion

        #region Object Properties

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

        #endregion

        #region Basic Construction

        /// <summary>
        /// Create an entity node from an internal object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, INodeWrangler nodeWrangler)
        {
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }
            
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;
            
            Inputs = new ObservableCollection<IPort>();
            Outputs = new ObservableCollection<IPort>();

            Type = PointerRefType.Internal;
        }
        
        /// <summary>
        /// Create an entity node from an internal object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(Type type, INodeWrangler nodeWrangler)
        {
            object obj = TypeLibrary.CreateObject(type.Name);

            EntityNodeWrangler entityWrangler = (EntityNodeWrangler)nodeWrangler;
            AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                entityWrangler.Asset.Objects,
                type,
                entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
            ((dynamic)obj).SetInstanceGuid(guid);

            Object = obj;
            
            byte[] b = guid.ExportedGuid.ToByteArray();
            uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
            TrySetProperty("Flags", value);
            
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;
            
            Inputs = new ObservableCollection<IPort>();
            Outputs = new ObservableCollection<IPort>();

            Type = PointerRefType.Internal;
        }

        /// <summary>
        /// Create an entity node from an external object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fileGuid"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, Guid fileGuid, INodeWrangler nodeWrangler)
        {
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            FileGuid = fileGuid;
            ClassGuid = InternalGuid.ExportedGuid;
            Type = PointerRefType.External;
            
            Inputs = new ObservableCollection<IPort>();
            Outputs = new ObservableCollection<IPort>();
        }

        public EntityNode()
        {
            Inputs = new ObservableCollection<IPort>();
            Outputs = new ObservableCollection<IPort>();
        }

        #endregion

        #region Static construction

        /// <summary>
        /// Gets a proper Node from an internal object. This will return subclasses of <see cref="EntityNode"/> if they are found.
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <param name="createId">Generate a unique ID based on the assets current GUIDs</param>
        /// <returns>The object as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(object entity, INodeWrangler wrangler, bool createId = false)
        {
            if (createId)
            {
                EntityNodeWrangler entityWrangler = (EntityNodeWrangler)wrangler;
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    entityWrangler.Asset.Objects,
                    entity.GetType(),
                    entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
            }
            
            if (ExtensionsManager.EntityNodeExtensions.ContainsKey(entity.GetType().Name))
            {
                // We need to construct it manually(since node extensions likely just have blank constructors)
                EntityNode node = (EntityNode)Activator.CreateInstance(ExtensionsManager.EntityNodeExtensions[entity.GetType().Name]);

                node.NodeWrangler = wrangler;
                node.Object = entity;
                
                node.Type = PointerRefType.Internal;
                node.ObjectType = entity.GetType().Name;
                node.RefreshCache();
                
                return node;
            }
            else if (EntityMappingNode.EntityMappings.ContainsKey(entity.GetType().Name))
            {
                EntityMappingNode node = new EntityMappingNode();

                node.Object = entity;
                node.NodeWrangler = wrangler;

                node.Type = PointerRefType.Internal;
                
                node.Load(entity.GetType().Name);

                return node;
            }
            else
            {
                return new EntityNode(entity, wrangler);
            }
        }
        
        /// <summary>
        /// Gets a proper Node from a type. This will return subclasses of <see cref="EntityNode"/> if they are found.  
        /// </summary>
        /// <param name="type">The object type this will be constructed off of</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <returns>The object type as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(Type type, INodeWrangler wrangler)
        {
            if (ExtensionsManager.EntityNodeExtensions.ContainsKey(type.Name))
            {
                // We need to construct it manually(since node extensions likely just have blank constructors)
                EntityNode node = (EntityNode)Activator.CreateInstance(ExtensionsManager.EntityNodeExtensions[type.Name]);

                node.NodeWrangler = wrangler;
                object entity = TypeLibrary.CreateObject(type.Name);
                
                EntityNodeWrangler entityWrangler = (EntityNodeWrangler)wrangler;
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    entityWrangler.Asset.Objects,
                    entity.GetType(),
                    entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
                
                if (TypeLibrary.IsSubClassOf(entity, "DataBusPeer"))
                {
                    byte[] b = guid.ExportedGuid.ToByteArray();
                    uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                    entity.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(entity, value);
                }
                
                node.Object = entity;
                
                node.Type = PointerRefType.Internal;
                node.RefreshCache();
                
                return node;
            }
            else if (EntityMappingNode.EntityMappings.ContainsKey(type.Name))
            {
                EntityMappingNode node = new EntityMappingNode();
                object entity = TypeLibrary.CreateObject(type.Name);
                
                EntityNodeWrangler entityWrangler = (EntityNodeWrangler)wrangler;
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    entityWrangler.Asset.Objects,
                    entity.GetType(),
                    entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
                
                if (TypeLibrary.IsSubClassOf(entity, "DataBusPeer"))
                {
                    byte[] b = guid.ExportedGuid.ToByteArray();
                    uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                    entity.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(entity, value);
                }
                
                node.Object = entity;
                node.NodeWrangler = wrangler;

                node.Type = PointerRefType.Internal;
                node.ObjectType = entity.GetType().Name;
                
                node.Load(entity.GetType().Name);

                return node;
            }
            else
            {
                return new EntityNode(type, wrangler);
            }
        }

        /// <summary>
        /// Gets a proper Node from an external object. This will return subclasses of <see cref="EntityNode"/> if they are found.  
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="fileGuid">The guid of the file this external object hails from</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <param name="createId">Generate a unique ID based on the assets current guids</param>
        /// <returns>The object as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(object entity, Guid fileGuid, INodeWrangler wrangler, bool createId = false)
        {
            if (createId)
            {
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(fileGuid)).Objects,
                    entity.GetType(),
                    fileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
                
                if (TypeLibrary.IsSubClassOf(entity, "DataBusPeer"))
                {
                    byte[] b = guid.ExportedGuid.ToByteArray();
                    uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                    entity.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(entity, value);
                }
            }
            
            if (ExtensionsManager.EntityNodeExtensions.ContainsKey(entity.GetType().Name))
            {
                EntityNode node = (EntityNode)Activator.CreateInstance(ExtensionsManager.EntityNodeExtensions[entity.GetType().Name]);
                
                node.Object = entity;
                node.NodeWrangler = wrangler;

                node.FileGuid = fileGuid;
                node.ClassGuid = ((dynamic)entity).GetInstanceGuid().ExportedGuid;
                node.Type = PointerRefType.External;
                node.ObjectType = entity.GetType().Name;
                node.RefreshCache();

                return node;
            }
            else if (EntityMappingNode.EntityMappings.ContainsKey(entity.GetType().Name))
            {
                EntityMappingNode node = new EntityMappingNode();
                
                node.Object = entity;
                node.NodeWrangler = wrangler;

                node.FileGuid = fileGuid;
                node.ClassGuid = ((dynamic)entity).GetInstanceGuid().ExportedGuid;
                node.Type = PointerRefType.External;
                
                node.Load(entity.GetType().Name);

                return node;
            }
            else
            {
                return new EntityNode(entity, fileGuid, wrangler);
            }
        }

        #endregion
        
        public override string ToString()
        {
            #if RELEASE___FINAL
            return $"{Header}";
            #else
            return $"{Realm} {Header} {InternalGuid.ToString()}";
            #endif
        }
    }
    
    public class ObjectFlagsHelper
    {
        public string GuidMask { get; set; }
        public bool ClientEvent { get; set; }
        public bool ServerEvent { get; set; }
        public bool ClientProperty { get; set; }
        public bool ServerProperty { get; set; }
        public bool ClientLinkSource { get; set; }
        public bool ServerLinkSource { get; set; }
        public bool UnusedFlag { get; set; }

        /// <summary>
        /// Credit to github.com/Mophead01 for the Object Flags parser
        /// </summary>
        /// <param name="flags"></param>
        public ObjectFlagsHelper(uint flags)
        {
            ClientEvent = Convert.ToBoolean((flags & 33554432) != 0 ? 1 : 0);
            ServerEvent = Convert.ToBoolean((flags & 67108864) != 0 ? 1 : 0);
            ClientProperty = Convert.ToBoolean((flags & 134217728) != 0 ? 1 : 0);
            ServerProperty = Convert.ToBoolean((flags & 268435456) != 0 ? 1 : 0);
            ClientLinkSource = Convert.ToBoolean((flags & 536870912) != 0 ? 1 : 0);
            ServerLinkSource = Convert.ToBoolean((flags & 1073741824) != 0 ? 1 : 0);
            UnusedFlag = Convert.ToBoolean((flags & 2147483648) != 0 ? 1 : 0);
            GuidMask = (flags & 33554431).ToString("X2").ToLower();
        }
        
        public static implicit operator uint(ObjectFlagsHelper flagsHelper) => flagsHelper.GetAsFlags();
        public static explicit operator ObjectFlagsHelper(uint flags) => new ObjectFlagsHelper(flags);

        /// <summary>
        /// I'm too lazy to do what I did with PropertyFlagsHelper so we will just use this method to get the flags instead
        /// Credit to github.com/Mophead01 for the Object Flags creation
        /// </summary>
        /// <returns></returns>
        public uint GetAsFlags()
        {
            bool isTooLarge = !uint.TryParse(GuidMask, NumberStyles.HexNumber, null, out var newFlags);
            if (isTooLarge || newFlags > 33554431)
            {
                newFlags = 0;
                App.Logger.LogWarning("Invalid Guid Mask");
            }

            if (ClientEvent)
            {
                newFlags |= 33554432;
            }
            if (ServerEvent)
            {
                newFlags |= 67108864;
            }
            if (ClientProperty)
            {
                newFlags |= 134217728;
            }
            if (ServerProperty)
            {
                newFlags |= 268435456;
            }
            if (ClientLinkSource)
            {
                newFlags |= 536870912;
            }
            if (ServerLinkSource)
            {
                newFlags |= 1073741824;
            }
            if (UnusedFlag)
            {
                newFlags |= 2147483648;
            }

            return newFlags;
        }
    }
}