using System;
using System.Windows;
using BlueprintEditorPlugin.BlueprintUtils;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using FrostyEditor;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.ComponentEditor.NodeWrangler
{
    public class ComponentNodeWrangler : EntityNodeWrangler
    {
        /// <summary>
        /// The root component of this asset
        /// </summary>
        public object RootComponent
        {
            get
            {
                PointerRef pointerRef = ((dynamic)Asset.RootObject).Object;
                return pointerRef.Internal;
            }
        }
        
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
                    ((dynamic)Asset.RootObject).Object.Internal.Components.Add(pointerRef);
                    
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
            UpdateComponentCount();
        }

        /// <summary>
        /// Adds a node as a component to another node
        /// </summary>
        /// <param name="entityNode"></param>
        public void AddChildComponent(EntityNode entityNode, EntityNode parentNode)
        {
            if (InternalNodeCache.ContainsKey(entityNode.InternalGuid))
            {
                App.Logger.LogError("An item with the AssetClassGuid {0} has already been added!", entityNode.InternalGuid.ToString());
                return;
            }

            if (parentNode.TryGetProperty("Components") == null)
            {
                App.Logger.LogError("Node {0} does not have any components, therefore cannot have children", parentNode.ObjectType ?? parentNode.Object.GetType().Name);
                return;
            }
                    
            InternalNodeCache.Add(((dynamic)entityNode).GetInstanceGuid(), entityNode);
            Asset.AddObject(entityNode.Object);
            PointerRef pointerRef = new PointerRef(entityNode.Object);
            ((dynamic)parentNode.Object).Components.Add(pointerRef);
            
            UpdateComponentCount();
            ModifyAsset();
            
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(entityNode);
            });

            entityNode.OnCreation();
        }

        public override void RemoveVertex(IVertex vertex)
        {
            if (vertex is INode node && !(vertex is IRedirect))
            {
                ClearConnections(node);
            }

            vertex.OnDestruction();
            
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Remove(vertex);
            });
            
            switch (vertex)
            {
                case EntityNode entityNode when entityNode.Type == PointerRefType.Internal:
                {
                    InternalNodeCache.Remove(entityNode.InternalGuid);
                    Asset.RemoveObject(entityNode.Object);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    ((dynamic)Asset.RootObject).Object.Internal.Components.Remove(pointerRef); // TODO: Remove from parent node components
                    ModifyAsset();
                    UpdateComponentCount();
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

        /// <summary>
        /// Recursively updates the component counts
        /// </summary>
        public void UpdateComponentCount()
        {
            // Not all component types have counts
            // Some for example are entirely based on the client and don't need to keep track as a result(e.g UI Widgets)
            if (Asset.RootObject.GetType().GetProperty("ClientRuntimeComponentCount") == null)
                return;
            
            foreach (PointerRef componentRef in ((dynamic)Asset.RootObject).Object.Internal.Components)
            {
                EntityNode node = GetEntityNode(((dynamic)componentRef.Internal).GetInstanceGuid());
                Realm realm = node.DetermineRealm();
                
                if (realm == Realm.Client || realm == Realm.ClientAndServer)
                {
                    ((dynamic)RootComponent).ClientRuntimeComponentCount++;
                }
                
                if (realm == Realm.Server || realm == Realm.ClientAndServer)
                {
                    ((dynamic)RootComponent).ServerRuntimeComponentCount++;
                }
                
                UpdateCountRecursively(componentRef.Internal);
            }
        }

        private void UpdateCountRecursively(object component)
        {
            if (component.GetType().GetProperty("Components") == null)
                return;
            
            foreach (PointerRef componentRef in ((dynamic)component).Components)
            {
                EntityNode node = GetEntityNode(((dynamic)componentRef.Internal).GetInstanceGuid());
                Realm realm = node.DetermineRealm();
                
                if (realm == Realm.Client || realm == Realm.ClientAndServer)
                {
                    ((dynamic)RootComponent).ClientRuntimeComponentCount++;
                }
                
                if (realm == Realm.Server || realm == Realm.ClientAndServer)
                {
                    ((dynamic)RootComponent).ServerRuntimeComponentCount++;
                }
                
                UpdateCountRecursively(componentRef.Internal);
            }
        }
    }
}