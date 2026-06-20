using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    /// <summary>
    /// Base class for connections found in a <see cref="BlueprintGraphEditor"/> which connect 2 ports together
    /// with networking features and <see cref="Object"/> which represents the original connections object.
    ///
    /// <seealso cref="EntityNodeWrangler"/>
    /// <seealso cref="EntityPort"/>
    /// </summary>
    public abstract class EntityConnection : BaseConnection, INetworked
    {
        private static readonly List<(Realm, Realm)> ImplicitConnectionCombos = new List<(Realm, Realm)>
        {
            (Realm.ClientAndServer, Realm.Server),
            (Realm.ClientAndServer, Realm.Client),
            (Realm.NetworkedClientAndServer, Realm.Server),
            (Realm.NetworkedClientAndServer, Realm.NetworkedClient),
            (Realm.Server, Realm.NetworkedClient),
            (Realm.NetworkedClient, Realm.Client),
            (Realm.Client, Realm.NetworkedClient),
            (Realm.Client, Realm.ClientAndServer),
            (Realm.Server, Realm.ClientAndServer),
            (Realm.Server, Realm.NetworkedClientAndServer),
            (Realm.NetworkedClientAndServer, Realm.ClientAndServer),
            (Realm.Server, Realm.Client)
        };

        /// <summary>
        /// Nodes never have things like <see cref="Realm"/>.NetworkedClient, but connections do.
        /// These are the same to nodes, as they do not differentiate between the 2
        /// </summary>
        public static readonly List<(Realm, Realm)> IdenticalTargetCombos = new List<(Realm, Realm)>
        {
            (Realm.Client, Realm.NetworkedClient),
            (Realm.NetworkedClient, Realm.Client),
            (Realm.ClientAndServer, Realm.NetworkedClientAndServer),
            (Realm.NetworkedClientAndServer, Realm.ClientAndServer)
        };

        public virtual Realm Realm { get; set; }

        #region Commands

        public ICommand EditCommand => new DelegateCommand(Edit);

        public virtual void Edit()
        {
            if (Type == ConnectionType.Property)
            {
                EditPropConnectionArgs editArgs = new EditPropConnectionArgs(this);
                MessageBoxResult result = EditPromptWindow.Show(editArgs, $"Edit {this}");
                if (result == MessageBoxResult.Yes)
                {
                    Realm = editArgs.Realm;
                    PropType = editArgs.PropertyType;
                }
            }
            else
            {
                EditConnectionArgs editArgs = new EditConnectionArgs(this);
                MessageBoxResult result = EditPromptWindow.Show(editArgs, $"Edit {this}");
                if (result == MessageBoxResult.Yes)
                {
                    Realm = editArgs.Realm;
                }
            }

            ((EntityNodeWrangler)Target.Node.NodeWrangler).ModifyAsset();
        }
        
        public ICommand RemoveCommand => new DelegateCommand(Remove);

        protected virtual void Remove()
        {
            if (Source.Node is IRedirect || Target.Node is IRedirect)
                return;
            
            Target.Node.NodeWrangler.RemoveConnection(this);
        }
        
        public ICommand FixCommand => new DelegateCommand(UserFix);

        public void UserFix()
        {
            EntityPort target = (EntityPort)Target;
            EntityPort source = (EntityPort)Source;
            
            FrostyTaskWindow.Show("Fixing problems...", "", task =>
            {
                bool synced = false; // Whether or not we have tried syncing the node to fix the problem
                
                // We get 16 attempts to solve all problems... Hopefully they work!
                for (int i = 0; i < 16; i++)
                {
                    if (CurrentStatus.Status == EditorStatus.Alright)
                    {
                        task.Update(null, 100.0);
                        break; // Yay!
                    }
                    
                    switch (CurrentStatus.ToolTip)
                    {
                        case "A connection cannot have its realm set to any":
                        {
                            if (target.DetermineRealm() != Realm.Any)
                            {
                                Realm = target.DetermineRealm();
                            }
                            else if (source.DetermineRealm() != Realm.Any)
                            {
                                Realm = source.DetermineRealm();
                            }
                            else
                            {
                                source.ForceFixRealm();
                                target.ForceFixRealm();
                                ForceFixRealm();
                            }
                        } break;
                        case "Connection source or target is not an EntityPort. Please use EntityPorts for Blueprints.":
                        {
                            Target.Node.NodeWrangler.RemoveConnection(this); // Fuck you
                            
                            #if DEVELOPER___DEBUG
                            App.Logger.LogError("Fuck you");
                            #endif
                        } break;
                        case "Connection realm should be the same as target realm":
                        {
                            if (target.Realm != Realm.Any || target.Realm != Realm.Invalid)
                            {
                                Realm = target.Realm;
                            }
                            else
                            {
                                target.ForceFixRealm();
                            }
                        } break;
                        case "Cannot implicitly determine the realms of this connection based on ports. Please manually set realms":
                        {
                            source.ForceFixRealm();
                            target.ForceFixRealm();
                            ForceFixRealm();
                        } break;
                        case "Property type is invalid":
                        {
                            if (target.IsInterface)
                            {
                                PropType = PropertyType.Interface;
                            }
                            else
                            {
                                PropType = PropertyType.Default;
                            }
                        } break;
                        case "Property type is set to interface, despite not plugging into an interface":
                        {
                            PropType = PropertyType.Default;
                        } break;
                        case "Connection realm is invalid!":
                        {
                            if (source.Realm != Realm.Any && source.Realm != Realm.Invalid)
                            {
                                Realm = source.Realm;
                            }
                            else if (source.Realm == Realm.Any)
                            {
                                Realm = source.DetermineRealm();
                            }
                            else
                            {
                                source.ForceFixRealm();
                                target.ForceFixRealm();
                                ForceFixRealm();
                            }
                        } break;
                        default:
                        {
                            if (!synced && CurrentStatus.ToolTip == "Client to Server is not a valid combination of realms" && Type == ConnectionType.Event)
                            {
                                EntityNode node = EntityNode.GetNodeFromEntity(TypeLibrary.CreateObject("EventSyncEntityData"), Source.Node.NodeWrangler, true);
                                node.Location = new Point(Source.Node.Location.X + Source.Node.Size.Width, Source.Node.Location.Y);
                                Source.Node.NodeWrangler.AddVertex(node);

                                // We need to get the EventSync's output regardless of if an NMC exists
                                EntityOutput output = node.GetOutput("Out", ConnectionType.Event);

                                EntityInput input = node.GetInput("Client", ConnectionType.Event);
                                
                                EventConnection eventConnection = new EventConnection((EventOutput)output, (EventInput)Target);
                                
                                Source.Node.NodeWrangler.AddConnection(eventConnection);
                                synced = true;

                                Target = input;
                                target = input;
                                
                                UpdateTargetRef(); // Make sure the update is synced in the ebx

                                break;
                            }
                            
                            if (CurrentStatus.ToolTip == $"{source.Realm} to {target.Realm} is not a valid combination of realms")
                            {
                                source.ForceFixRealm();
                                target.ForceFixRealm();
                            }
                            ForceFixRealm();
                        } break;
                    }
                    
                    task.Update(null, i / 16.0);
                    UpdateStatus();
                }

                if (CurrentStatus.Status == EditorStatus.Flawed || CurrentStatus.Status == EditorStatus.Broken)
                {
                    if (CurrentStatus.ToolTip == $"{source.Realm} to {target.Realm} is not a valid combination of realms")
                    {
                        App.Logger.LogError($"Unable to implicitly determine intended realms for {source} to {target}, please manually update their realms.");
                        return;
                    }
                    App.Logger.LogError("Unable to solve all problems with this connection. Please either manually solve issues, or try again.");
                }
            });
        }

        #endregion

        #region Entity data

        public abstract ConnectionType Type { get; }
        public object Object { get; set; }

        private bool _hasPlayer;
        public bool HasPlayer
        {
            get => _hasPlayer;
            set
            {
                _hasPlayer = value;
                NotifyPropertyChanged(nameof(HasPlayer));
            } 
        }

        private PropertyType _propType;
        public PropertyType PropType
        {
            get => _propType;
            set
            {
                _propType = value;
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType);
                NotifyPropertyChanged(nameof(PropType));
                UpdateStatus();
            }
        }

        /// <summary>
        /// Updates the <see cref="Object"/>'s target pointer ref to that of the current <see cref="BaseConnection.Source"/> value
        /// <seealso cref="FixRealm"/>
        /// <seealso cref="ForceFixRealm"/>
        /// </summary>
        public void UpdateSourceRef()
        {
            UpdateSourceRef((EntityPort)Source);
        }

        /// <summary>
        /// Updates the <see cref="Object"/>'s source pointer ref to that of the specified entity port
        /// <seealso cref="FixRealm"/>
        /// <seealso cref="ForceFixRealm"/>
        /// </summary>
        /// <param name="source"></param>
        public virtual void UpdateSourceRef(EntityPort source)
        {
            HasPlayer = source.HasPlayer;
            if (source.IsInterface)
            {
                PropType = PropertyType.Interface;
            }
        }

        /// <summary>
        /// Updates the <see cref="Object"/>'s target pointer ref to that of the current <see cref="BaseConnection.Target"/> value
        /// <seealso cref="FixRealm"/>
        /// <seealso cref="ForceFixRealm"/>
        /// </summary>
        public void UpdateTargetRef()
        {
            UpdateTargetRef((EntityPort)Target);
        }

        /// <summary>
        /// Updates the <see cref="Object"/>'s target pointer ref to that of the specified entity port
        /// <seealso cref="FixRealm"/>
        /// <seealso cref="ForceFixRealm"/>
        /// </summary>
        /// <param name="target"></param>
        public virtual void UpdateTargetRef(EntityPort target)
        {
            HasPlayer = target.HasPlayer;
            if (target.IsInterface)
            {
                PropType = PropertyType.Interface;
            }
        }

        #endregion

        #region Networked implementation

        public Realm ParseRealm(object obj)
        {
            // TODO: this is a fatty! Please make it smaller!
            switch ((string)obj)
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
                case "EventConnectionTargetType_ClientAndServer":
                {
                    return Realm.ClientAndServer;
                }
                case "EventConnectionTargetType_Client":
                {
                    return Realm.Client;
                }
                case "EventConnectionTargetType_Server":
                {
                    return Realm.Server;
                }
                case "EventConnectionTargetType_NetworkedClient":
                {
                    return Realm.NetworkedClient;
                }
                case "EventConnectionTargetType_NetworkedClientAndServer":
                {
                    return Realm.NetworkedClientAndServer;
                }
                default:
                {
                    return Realm.Invalid;
                }
            }
        }

        public Realm DetermineRealm(bool ignoreCurrent = false)
        {
            Realm realm = Realm;

            if ((Realm == Realm.Any || Realm == Realm.Invalid) || ignoreCurrent)
            {
                EntityPort source = (EntityPort)Source;
                EntityPort target = (EntityPort)Target;

                Realm sourceRealm = source.DetermineRealm();
                Realm targetRealm = target.DetermineRealm();
                if (targetRealm != Realm.Any && targetRealm != Realm.Invalid)
                {
                    realm = targetRealm;
                }
                else if (sourceRealm != Realm.Any && sourceRealm != Realm.Invalid)
                {
                    realm = sourceRealm;
                }
                // Fuck you
                else
                {
                    source.FixRealm();
                    target.FixRealm();
                
                    if (target.Realm != Realm.Any && target.Realm != Realm.Invalid)
                    {
                        realm = target.Realm;
                    }
                }
            }

            return realm;
        }
        
        public void FixRealm()
        {
            Realm = DetermineRealm();
            ((EntityNodeWrangler)Target.Node.NodeWrangler).ModifyAsset();
        }

        public void ForceFixRealm()
        {
            Realm = DetermineRealm(true);
            ((EntityNodeWrangler)Target.Node.NodeWrangler).ModifyAsset();
        }

        #endregion

        public override void UpdateStatus()
        {
            SetStatus(EditorStatus.Alright, "");
            
            if (Realm == Realm.Invalid)
            {
                SetStatus(EditorStatus.Broken, "Connection realm is invalid!");
                return;
            }

            EntityPort source = Source as EntityPort;
            EntityPort target = Target as EntityPort;

            if (source == null || target == null)
            {
                SetStatus(EditorStatus.Broken, "Connection source or target is not an EntityPort. Please use EntityPorts for Blueprints.");
                
                #if DEVELOPER___DEBUG
                App.Logger.LogError("HEY DUMBASS YOU'RE USING THE WRONG TYPES FOR CONNECTION {0} USE ENTITYPORTS INSTEAD GOD DAMMIT", ToString());
                #endif
                return;
            }
            
            if (source.Realm == Realm.Any && target.Realm == Realm.Any)
            {
                // If this is the case that means the realm can be determined
                if (source.DetermineRealm() == Realm.Any && target.DetermineRealm() == Realm.Any)
                {
                    SetStatus(EditorStatus.Flawed, "Cannot implicitly determine the realms of this connection based on ports. Please manually set realms");
                }
            }

            // Links don't have realms, so naturally they are probably gonna be set to any.
            if (Realm == Realm.Any && Type != ConnectionType.Link)
            {
                SetStatus(EditorStatus.Broken, "A connection cannot have its realm set to any");
                return;
            }

            if (source.Realm != target.Realm && !ImplicitConnectionCombos.Contains((source.Realm, target.Realm)) && (source.Realm != Realm.Any && target.Realm != Realm.Any))
            {
                SetStatus(EditorStatus.Flawed, $"{source.Realm} to {target.Realm} is not a valid combination of realms");
            }
        }

        #region Construction

        public EntityConnection(IPort source, IPort target, object obj) : base(source, target)
        {
            Object = obj;
        }
        
        public EntityConnection(IPort source, IPort target) : base(source, target)
        {
        }

        protected EntityConnection()
        {
        }

        #endregion

        public override string ToString()
        {
            return $"{Source.Node}({Source.Name}) -> {Target.Node}({Target.Name})";
        }
    }

    public enum ConnectionType
    {
        Event = 0,
        Link = 1,
        Property = 2
    }
}