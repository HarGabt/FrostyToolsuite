using System;
using System.Collections.Generic;
using System.Windows;
using BlueprintEditorPlugin.BlueprintUtils;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using BlueprintEditorPlugin.Windows;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.UIWidgetEditor.NodeWrangler
{
    public class UIWidgetNodeWrangler : EntityNodeWrangler
    {
        public Dictionary<string, EntityNode> LayerNameCache = new();

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
                    
                    if (TypeLibrary.IsSubClassOf(entityNode.Object, "UIElementEntityData"))
                    {
                        AddElementArgs args = new AddElementArgs();
                        MessageBoxResult result = EditPromptWindow.Show(args);
                        if (result != MessageBoxResult.Yes)
                            return;

                        if (!LayerNameCache.ContainsKey(args.LayerName))
                        {
                            App.Logger.LogError("Unable to find layer {0}", args.LayerName);
                            return;
                        }
                        
                        Asset.AddObject(entityNode.Object);
                        InternalNodeCache.Add(entityNode.InternalGuid, entityNode);
                        PointerRef pointerRef = new PointerRef(entityNode.Object);
                        
                        EntityNode layer = LayerNameCache[args.LayerName];
                        ((dynamic)layer.Object).Elements.Add(pointerRef);
                    }
                    else
                    {
                        Asset.AddObject(entityNode.Object);
                        InternalNodeCache.Add(entityNode.InternalGuid, entityNode);
                        PointerRef pointerRef = new PointerRef(entityNode.Object);
                    
                        if (entityNode.ObjectType == "UIElementLayerEntityData")
                        {
                            ((dynamic)Asset.RootObject).Object.Internal.Layers.Add(pointerRef);
                            LayerNameCache.Add(entityNode.TryGetProperty("LayerName").ToString(), entityNode);
                        }
                        else
                        {
                            ((dynamic)Asset.RootObject).Object.Internal.Components.Add(pointerRef);
                        }
                    }

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

        public override void RemoveVertex(IVertex vertex)
        {
            if (vertex is INode node && !(vertex is IRedirect))
            {
                ClearConnections(node);
            }

            vertex.OnDestruction();
            
            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Remove(vertex);
            });
            
            switch (vertex)
            {
                case EntityNode entityNode:
                {
                    InternalNodeCache.Remove(entityNode.InternalGuid);
                    Asset.RemoveObject(entityNode.Object);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    
                    if (TypeLibrary.IsSubClassOf(entityNode.Object, "UIElementEntityData"))
                    {
                        EntityNode layerNode = GetLayerFromNode(entityNode);
                        if (layerNode == null)
                            return;
                        
                        dynamic elements = layerNode.TryGetProperty("Elements");
                        elements.Remove(pointerRef);
                    }
                    else
                    {
                        if (entityNode.ObjectType == "UIElementLayerEntityData")
                        {
                            ((dynamic)Asset.RootObject).Object.Internal.Layers.Remove(pointerRef);
                            foreach (EntityNode elementNode in GetLayerElementNodes(entityNode))
                            {
                                RemoveVertex(elementNode); // TODO: Big stutter issue? We're doing a lot with this we don't need to
                            }

                            LayerNameCache.Remove(entityNode.TryGetProperty("LayerName").ToString());
                        }
                        else
                        {
                            ((dynamic)Asset.RootObject).Object.Internal.Components.Remove(pointerRef);
                        }
                    }
                    
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

        /// <summary>
        /// Gets the UIElementLayer that the <see cref="EntityNode"/>.Object is assigned to
        /// </summary>
        /// <param name="node"></param>
        /// <returns>The <see cref="EntityNode"/> of containing the layer, null if not found</returns>
        private EntityNode GetLayerFromNode(EntityNode node)
        {
            foreach (EntityNode layerNode in LayerNameCache.Values)
            {
                dynamic elements = layerNode.TryGetProperty("Elements");
                if (elements == null)
                    continue;

                foreach (dynamic element in elements)
                {
                    if (element.Internal.GetInstanceGuid() == node.InternalGuid)
                        return layerNode;
                }
            }
            
            return null;
        }

        private List<EntityNode> GetLayerElementNodes(EntityNode layerNode)
        {
            List<EntityNode> elementNodes = new List<EntityNode>();
            dynamic elements = layerNode.TryGetProperty("Elements");
            if (elements == null)
                return elementNodes;

            foreach (dynamic element in elements)
            {
                EntityNode node = GetEntityNode((AssetClassGuid)element.GetInstanceGuid());
                
                // FUCK!
                if (node == null)
                    continue;
                
                elementNodes.Add(node);
            }

            return elementNodes;
        }
    }

    public class AddElementArgs
    {
        [DisplayName("Layer Name")]
        [Description("The name of the layer this UI element is assigned to")]
        public string LayerName { get; set; }
        
        [DisplayName("Element Name")]
        [Description("The name of the element")]
        public string ElementName { get; set; }

        public AddElementArgs()
        {
            LayerName = "";
            ElementName = "";
        }
    }
}