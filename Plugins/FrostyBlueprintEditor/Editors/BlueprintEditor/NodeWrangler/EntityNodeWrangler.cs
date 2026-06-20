using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.BlueprintUtils;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler
{
    /// <summary>
    /// This class manages all of the <see cref="EntityNode"/>s, <see cref="EntityConnection"/>s, and other <see cref="IVertex"/>es in a graph.
    ///
    /// <seealso cref="BlueprintGraphEditor"/>
    /// </summary>
    public class EntityNodeWrangler : BaseNodeWrangler, IEbxNodeWrangler
    {
        public EbxAsset Asset { get; set; }
        public EbxAssetEntry AssetEntry => App.AssetManager.GetEbxEntry(Asset.FileGuid);
        public AssetClassGuid InterfaceGuid { get; set; }
        protected readonly Dictionary<AssetClassGuid, EntityNode> InternalNodeCache = new();
        protected readonly Dictionary<(Guid, Guid), EntityNode> ExternalNodeCache = new();

        protected readonly Dictionary<int, InterfaceNode> InterfacePiCache = new();
        protected readonly Dictionary<int, InterfaceNode> InterfaceLiCache = new();
        protected readonly Dictionary<int, InterfaceNode> InterfaceEiCache = new();
        protected readonly Dictionary<int, InterfaceNode> InterfacePoCache = new();
        protected readonly Dictionary<int, InterfaceNode> InterfaceLoCache = new();
        protected readonly Dictionary<int, InterfaceNode> InterfaceEoCache = new();

        #region Transient edits

        /// <summary>
        /// Add a node to the Graph Editor without editing EBX
        /// </summary>
        /// <param name="vert"></param>
        public void AddVertexTransient(IVertex vert)
        {
            switch (vert)
            {
                case EntityNode entityNode when entityNode.Type == PointerRefType.Internal:
                {
                    if (InternalNodeCache.ContainsKey(entityNode.InternalGuid))
                    {
                        App.Logger.LogError("An item with the AssetClassGuid {0} has already been added!", entityNode.InternalGuid.ToString());
                        return;
                    }
                    InternalNodeCache.Add(entityNode.InternalGuid, entityNode);
                } break;
                case EntityNode entityNode:
                {
                    if (ExternalNodeCache.ContainsKey((entityNode.FileGuid, entityNode.ClassGuid)))
                    {
                        App.Logger.LogError("Multiple imported items with the same guids detected!");
                        return;
                    }
                    ExternalNodeCache.Add((entityNode.FileGuid, entityNode.ClassGuid), entityNode);
                } break;
                case InterfaceNode interfaceNode:
                {
                    if (interfaceNode.Inputs.Count != 0)
                    {
                        switch (interfaceNode.ConnectionType)
                        {
                            case ConnectionType.Event:
                            {
                                if (InterfaceEiCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                InterfaceEiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                            case ConnectionType.Link:
                            {
                                if (InterfaceLiCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                InterfaceLiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                            case ConnectionType.Property:
                            {
                                if (InterfacePiCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                InterfacePiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                        }
                    }
                    else
                    {
                        switch (interfaceNode.ConnectionType)
                        {
                            case ConnectionType.Event:
                            {
                                if (InterfaceEoCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                    return;
                                }
                                InterfaceEoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                            case ConnectionType.Link:
                            {
                                if (InterfaceLoCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                    return;
                                }
                                InterfaceLoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                            case ConnectionType.Property:
                            {
                                if (InterfacePoCache.ContainsKey(HashingUtils.SmartHashString(interfaceNode.Header)))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                    return;
                                }
                                InterfacePoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            } break;
                        }
                    }
                } break;
            }
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(vert);
            });
            
            // Incase extensions aren't threadsafe
            Application.Current.Dispatcher.Invoke(vert.OnCreation);
        }

        #region Adding Connections

        public void AddConnectionTransient(IConnection connection)
        {
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
        }
        
        public void AddConnectionTransient(EntityOutput source, EntityInput target, object obj)
        {
            EntityConnection connection = null;
            switch (source.Type)
            {
                case ConnectionType.Event:
                {
                    connection = new EventConnection((EventOutput)source, (EventInput)target, obj);
                } break;
                case ConnectionType.Link:
                {
                    connection = new LinkConnection((LinkOutput)source, (LinkInput)target, obj);
                } break;
                case ConnectionType.Property:
                {
                    connection = new PropertyConnection((PropertyOutput)source, (PropertyInput)target, obj);
                } break;
            }
            
            if (source.Realm == Realm.Invalid)
            {
                source.Realm = connection.Realm;
            }

            if (target.Realm == Realm.Invalid)
            {
                target.Realm = connection.Realm;
            }
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
        }
        
        public void AddConnectionTransient(EntityOutput source, EntityInput target)
        {
            EntityConnection connection = null;
            switch (source.Type)
            {
                case ConnectionType.Event:
                {
                    connection = new EventConnection((EventOutput)source, (EventInput)target);
                } break;
                case ConnectionType.Link:
                {
                    connection = new LinkConnection((LinkOutput)source, (LinkInput)target);
                } break;
                case ConnectionType.Property:
                {
                    connection = new PropertyConnection((PropertyOutput)source, (PropertyInput)target);
                } break;
            }
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
        }

        #endregion

        #region Removing connections

        public void RemoveConnectionTransient(EntityConnection connection)
        {
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Remove(connection);
            });
        }

        #endregion

        #endregion

        #region Getting entity nodes

        public EntityNode GetEntityNode(AssetClassGuid internalId)
        {
            if (!InternalNodeCache.ContainsKey(internalId))
                return null;
            
            return InternalNodeCache[internalId];
        }

        public EntityNode GetEntityNode(Guid fileGuid, Guid assetClassGuid)
        {
            if (!ExternalNodeCache.ContainsKey((fileGuid, assetClassGuid)))
                return null;
            
            return ExternalNodeCache[(fileGuid, assetClassGuid)];
        }

        public EntityNode GetEntityNode(Guid fileGuid, AssetClassGuid internalId)
        {
            if (!ExternalNodeCache.ContainsKey((fileGuid, internalId.ExportedGuid)))
                return null;
            
            return ExternalNodeCache[(fileGuid, internalId.ExportedGuid)];
        }

        #endregion

        #region Getting InterfaceNodes

        public InterfaceNode GetInterfaceNode(string name, PortDirection direction, ConnectionType type)
        {
            if (direction == PortDirection.In)
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        if (!InterfaceEiCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfaceEiCache[HashingUtils.SmartHashString(name)];
                    }
                    case ConnectionType.Link:
                    {
                        if (!InterfaceLiCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfaceLiCache[HashingUtils.SmartHashString(name)];
                    }
                    case ConnectionType.Property:
                    {
                        if (!InterfacePiCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfacePiCache[HashingUtils.SmartHashString(name)];
                    }
                }
            }
            else
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        if (!InterfaceEoCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfaceEoCache[HashingUtils.SmartHashString(name)];
                    }
                    case ConnectionType.Link:
                    {
                        if (!InterfaceLoCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfaceLoCache[HashingUtils.SmartHashString(name)];
                    }
                    case ConnectionType.Property:
                    {
                        if (!InterfacePoCache.ContainsKey(HashingUtils.SmartHashString(name)))
                        {
                            return null;
                        }
                        return InterfacePoCache[HashingUtils.SmartHashString(name)];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the input interface cache so that the keys align with the names of the interfaces
        /// </summary>
        public void UpdateInputInterfaceCache()
        {
            // Properties
            List<int> keysToUpdate = new List<int>();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfacePiCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfacePiCache[key];
                InterfacePiCache.Remove(key);
                InterfacePiCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
            
            // Links
            keysToUpdate.Clear();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfaceLiCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfaceLiCache[key];
                InterfaceLiCache.Remove(key);
                InterfaceLiCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
            
            // Events
            keysToUpdate.Clear();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfaceEiCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfaceEiCache[key];
                InterfaceEiCache.Remove(key);
                InterfaceEiCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
        }
        
        /// <summary>
        /// Updates the input interface cache so that the keys align with the names of the interfaces
        /// </summary>
        public void UpdateOutputInterfaceCache()
        {
            // Properties
            List<int> keysToUpdate = new List<int>();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfacePoCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfacePoCache[key];
                InterfacePoCache.Remove(key);
                InterfacePoCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
            
            // Links
            keysToUpdate.Clear();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfaceLoCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfaceLoCache[key];
                InterfaceLoCache.Remove(key);
                InterfaceLoCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
            
            // Events
            keysToUpdate.Clear();
            foreach (KeyValuePair<int,InterfaceNode> keyValuePair in InterfaceEoCache)
            {
                if (keyValuePair.Key == HashingUtils.SmartHashString(keyValuePair.Value.Header))
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (int key in keysToUpdate)
            {
                InterfaceNode node = InterfaceEoCache[key];
                InterfaceEoCache.Remove(key);
                InterfaceEoCache.Add(HashingUtils.SmartHashString(node.Header), node);
            }
        }

        #endregion

        #region Adding Nodes

        public override void AddVertex(IVertex node)
        {
            switch (node)
            {
                case EntityNode entityNode:
                {
                    if (InternalNodeCache.ContainsKey(entityNode.InternalGuid))
                    {
                        App.Logger.LogError("An item with the AssetClassGuid {0} has already been added!", entityNode.InternalGuid.ToString());
                        return;
                    }
                    
                    Asset.AddObject(entityNode.Object);
                    InternalNodeCache.Add(entityNode.InternalGuid, entityNode);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    ((dynamic)Asset.RootObject).Objects.Add(pointerRef);
                    
                    ModifyAsset();
                } break;
                case InterfaceNode interfaceNode:
                {
                    switch (interfaceNode.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                intrfc.OutputEvents.Add((dynamic)interfaceNode.SubObject);
                                InterfaceEiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                            else
                            {
                                intrfc.InputEvents.Add((dynamic)interfaceNode.SubObject);
                                InterfaceEoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                        } break;
                        case ConnectionType.Link:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                intrfc.OutputLinks.Add((dynamic)interfaceNode.SubObject);
                                InterfaceLiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                            else
                            {
                                intrfc.InputLinks.Add((dynamic)interfaceNode.SubObject);
                                InterfaceLoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                        } break;
                        case ConnectionType.Property:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            intrfc.Fields.Add((dynamic)interfaceNode.SubObject);
                            
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                InterfacePiCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                            else
                            {
                                InterfacePoCache.Add(HashingUtils.SmartHashString(interfaceNode.Header), interfaceNode);
                            }
                        } break;
                    }
                    
                    ModifyAsset();
                } break;
            }

            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(node);
            });

            node.OnCreation();
        }

        #endregion

        #region Removing Nodes

        public override void RemoveVertex(IVertex node)
        {
            base.RemoveVertex(node);
            switch (node)
            {
                case EntityNode entityNode when entityNode.Type == PointerRefType.Internal:
                {
                    InternalNodeCache.Remove(entityNode.InternalGuid);
                    Asset.RemoveObject(entityNode.Object);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    ((dynamic)Asset.RootObject).Objects.Remove(pointerRef);
                    ModifyAsset();
                } break;
                case InterfaceNode interfaceNode:
                {
                    switch (interfaceNode.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                InterfaceEiCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).OutputEvents.Remove((dynamic)interfaceNode.SubObject);
                            }
                            else
                            {
                                InterfaceEoCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).InputEvents.Remove((dynamic)interfaceNode.SubObject);
                            }
                        } break;
                        case ConnectionType.Link:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                InterfaceLiCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).OutputLinks.Remove((dynamic)interfaceNode.SubObject);
                            }
                            else
                            {
                                InterfaceLoCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).InputLinks.Remove((dynamic)interfaceNode.SubObject);
                            }
                        } break;
                        case ConnectionType.Property:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                InterfacePiCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).Fields.Remove((dynamic)interfaceNode.SubObject);
                                if (((dynamic)interfaceNode.SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                                {
                                    InterfaceNode interfaceNode2 = GetInterfaceNode(interfaceNode.Header, PortDirection.Out, ConnectionType.Property);
                                    
                                    if (interfaceNode2 == null)
                                        break;
                                    
                                    RemoveVertex(interfaceNode2);
                                }
                            }
                            else
                            {
                                InterfacePoCache.Remove(HashingUtils.SmartHashString(interfaceNode.Header));
                                ((dynamic)interfaceNode.Object).Fields.Remove((dynamic)interfaceNode.SubObject);
                                if (((dynamic)interfaceNode.SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                                {
                                    InterfaceNode interfaceNode2 = GetInterfaceNode(interfaceNode.Header, PortDirection.In, ConnectionType.Property);
                                    
                                    if (interfaceNode2 == null)
                                        break;
                                    
                                    RemoveVertex(interfaceNode2);
                                }
                            }
                        } break;
                    }
                } break;
            }
        }

        #endregion

        #region Getting connections

        /// <summary>
        /// Gets a list of real connections, so not <see cref="TransientConnection"/>
        /// </summary>
        /// <returns></returns>
        public List<EntityConnection> GetRealConnections()
        {
            List<EntityConnection> entityConnections = new List<EntityConnection>();

            foreach (EntityConnection connection in Connections)
            {
                if (connection is TransientConnection)
                    continue;
                
                entityConnections.Add(connection);
            }

            return entityConnections;
        }

        #endregion

        #region Creating connections

        public override void AddConnection(IConnection connection)
        {
            base.AddConnection(connection);
            
            // If this connection is transient we shouldn't saved it
            if (connection is TransientConnection)
                return;
            
            EntityConnection entityConnection = (EntityConnection)connection;
            switch (entityConnection.Type)
            {
                case ConnectionType.Event:
                {
                    EventConnection eventConnection = ((EventConnection)connection);
                    ((dynamic)Asset.RootObject).EventConnections.Add((dynamic)eventConnection.Object);
                } break;
                case ConnectionType.Link:
                {
                    LinkConnection linkConnection = (LinkConnection)connection;
                    ((dynamic)Asset.RootObject).LinkConnections.Add((dynamic)linkConnection.Object);
                } break;
                case ConnectionType.Property:
                {
                    PropertyConnection propertyConnection = ((PropertyConnection)connection);
                    ((dynamic)Asset.RootObject).PropertyConnections.Add((dynamic)propertyConnection.Object);
                } break;
            }
            
            connection.Source.Node.OnOutputUpdated(connection.Source);
            connection.Target.Node.OnInputUpdated(connection.Target);

            ModifyAsset();
        }

        #endregion

        #region Removing connections

        public override void RemoveConnection(IConnection connection)
        {
            base.RemoveConnection(connection);

            if (connection is TransientConnection)
                return;

            dynamic root = (dynamic)Asset.RootObject;
            EntityConnection entityConnection = (EntityConnection)connection;

            switch (entityConnection)
            {
                case EventConnection eventConnection:
                {
                    root.EventConnections.Remove((dynamic)entityConnection.Object);
                    if (eventConnection.HasPlayer)
                    {
                        eventConnection.HasPlayer = false; // Set this to false so it stops bitching
                        if (eventConnection.Target.Node is EntityNode entityNode)
                        {
                            entityNode.HasPlayerEvent = GetConnections(entityNode).Any(alt => alt != connection && alt is EntityConnection entConnection && entConnection.HasPlayer);
                        }
                    }
                } break;
                case LinkConnection linkConnection:
                {
                    root.LinkConnections.Remove((dynamic)entityConnection.Object);
                } break;
                case PropertyConnection propertyConnection:
                {
                    root.PropertyConnections.Remove((dynamic)entityConnection.Object);
                } break;
            }
            
            ModifyAsset();
        }

        #endregion

        public void ModifyAsset()
        {
            if (!AssetEntry.IsDirty)
            {
                App.AssetManager.ModifyEbx(AssetEntry.Name, Asset);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Frosty.Core.App.EditorWindow.DataExplorer.RefreshItems();
                });
            }
            
            Asset.Update();

            if (AssetEntry.HasModifiedData)
            {
                AssetEntry.ModifiedEntry.DependentAssets.Clear();
                AssetEntry.ModifiedEntry.DependentAssets.AddRange(Asset.Dependencies);
            }
        }

        public EntityNodeWrangler()
        {
            PendingConnection = new EntityPendingConnection(this);
            RemoveConnectionsCommand = new DelegateCommand<IPort>(ClearConnections);
        }
    }
    
    public class EntityPendingConnection : BasePendingConnection
    {
        public ConnectionType Type { get; set; }
        
        public EntityPendingConnection(EntityNodeWrangler wrangler) : base(wrangler)
        {
            Start = new DelegateCommand<IPort>(source =>
            {
                Source = source;
                Type = ((EntityPort)source).Type;
                NotifyPropertyChanged(nameof(Type));
                NotifyPropertyChanged(nameof(CurvePoint1));
                NotifyPropertyChanged(nameof(CurvePoint2));
            });
            Finish = new DelegateCommand<IPort>(target =>
            {
                if (target == null)
                    return;
                
                EntityPort source = (EntityPort)Source;

                if (source.Type != ((EntityPort)target).Type)
                    return;
                
                if (source.RedirectNode != null || target.RedirectNode != null)
                    return;

                switch (Source.Direction)
                {
                    case PortDirection.Out when target.Direction == PortDirection.In:
                    {
                        switch (source.Type)
                        {
                            case ConnectionType.Event:
                            {
                                wrangler.AddConnection(new EventConnection((EventOutput)source, (EventInput)target));
                            }
                                break;
                            case ConnectionType.Link:
                            {
                                wrangler.AddConnection(new LinkConnection((LinkOutput)source, (LinkInput)target));
                            }
                                break;
                            case ConnectionType.Property:
                            {
                                wrangler.AddConnection(new PropertyConnection((PropertyOutput)source,
                                    (PropertyInput)target));
                            }
                                break;
                        }
                    } break;
                    case PortDirection.In when target.Direction == PortDirection.Out:
                    {
                        switch (source.Type)
                        {
                            case ConnectionType.Event:
                            {
                                wrangler.AddConnection(new EventConnection((EventOutput)target, (EventInput)source));
                            }
                                break;
                            case ConnectionType.Link:
                            {
                                wrangler.AddConnection(new LinkConnection((LinkOutput)target, (LinkInput)source));
                            }
                                break;
                            case ConnectionType.Property:
                            {
                                wrangler.AddConnection(new PropertyConnection((PropertyOutput)target,
                                    (PropertyInput)source));
                            }
                                break;
                        }
                    } break;
                }
            });
        }
    }
}
