using System;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.ComponentEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.CheapGraph;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.ComponentEditor
{
    /// <summary>
    /// Base implementation of a <see cref="BlueprintGraphEditor"/> which can edit assets with Components
    /// </summary>
    public class ComponentGraphEditor : BlueprintGraphEditor
    {
        public override bool IsValid(EbxAssetEntry assetEntry)
        {
            EbxAsset asset = App.AssetManager.GetEbx(assetEntry);
            Type assetType = asset.RootObject.GetType();

            // Let the UI editor handle this
            if (assetType.Name == "UIWidgetBlueprint" && ProfilesLibrary.ProfileName != "starwarsbattlefrontii")
            {
                return false;
            }
            
            // We only check property connections since we can assume if it has property connections it has everything else
            if (assetType.GetProperty("Object") != null 
                && assetType.GetProperty("PropertyConnections") != null 
                && assetType.GetProperty("Interface") != null)
            {
                Type objectType = ((dynamic)asset.RootObject).Object.Internal.GetType();
                
                if (objectType.GetProperty("Components") == null)
                    return false;
                
                return true;
            }
            
            return false;
        }

        public ComponentGraphEditor()
        {
            NodeWrangler = new ComponentNodeWrangler();
            NodePropertyGrid.NodeWrangler = NodeWrangler;
        }

        public override void LoadAsset(EbxAssetEntry assetEntry)
        {
            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            wrangler.Asset = App.AssetManager.GetEbx(assetEntry);
            
            EntityLayoutManager layoutManager = ExtensionsManager.GetValidLayoutManager(assetEntry);
            if (layoutManager != null)
            {
                LayoutManager = layoutManager;
                LayoutManager.NodeWrangler = NodeWrangler;
            }

            CheapMethod cheap = new CheapMethod(NodeWrangler);
            foreach (object assetObject in wrangler.Asset.Objects)
            {
                if (assetObject == wrangler.Asset.RootObject)
                    continue;

                if (assetObject.GetType().Name == "InterfaceDescriptorData")
                {
                    wrangler.InterfaceGuid = ((dynamic)assetObject).GetInstanceGuid();
                    foreach (dynamic field in ((dynamic)assetObject).Fields)
                    {
                        switch (field.AccessType.ToString())
                        {
                            case "FieldAccessType_Source":
                            {
                                wrangler.AddVertexTransient(new InterfaceNode(assetObject, field, field.Name,
                                    ConnectionType.Property, PortDirection.In, NodeWrangler));
                            } break;
                            case "FieldAccessType_Target":
                            {
                                wrangler.AddVertexTransient(new InterfaceNode(assetObject, field, field.Name,
                                    ConnectionType.Property, PortDirection.Out, NodeWrangler));
                            } break;
                            case "FieldAccessType_SourceAndTarget":
                            {
                                wrangler.AddVertexTransient(new InterfaceNode(assetObject, field, field.Name,
                                    ConnectionType.Property, PortDirection.In, NodeWrangler));
                                wrangler.AddVertexTransient(new InterfaceNode(assetObject, field, field.Name,
                                    ConnectionType.Property, PortDirection.Out, NodeWrangler));
                            } break;
                        }
                        cheap.SortGraph(wrangler.Vertices.Last());
                    }

                    foreach (dynamic inputEvent in ((dynamic)assetObject).InputEvents)
                    {
                        wrangler.AddVertexTransient(new InterfaceNode(assetObject, inputEvent, inputEvent.Name,
                            ConnectionType.Event, PortDirection.Out, NodeWrangler));
                        cheap.SortGraph(wrangler.Vertices.Last());
                    }
                    foreach (dynamic outputEvent in ((dynamic)assetObject).OutputEvents)
                    {
                        wrangler.AddVertexTransient(new InterfaceNode(assetObject, outputEvent, outputEvent.Name,
                            ConnectionType.Event, PortDirection.In, NodeWrangler));
                        cheap.SortGraph(wrangler.Vertices.Last());
                    }
                    
                    foreach (dynamic inputLink in ((dynamic)assetObject).InputLinks)
                    {
                        wrangler.AddVertexTransient(new InterfaceNode(assetObject, inputLink, inputLink.Name,
                            ConnectionType.Link, PortDirection.Out, NodeWrangler));
                        cheap.SortGraph(wrangler.Vertices.Last());
                    }
                    foreach (dynamic outputLink in ((dynamic)assetObject).OutputLinks)
                    {
                        wrangler.AddVertexTransient(new InterfaceNode(assetObject, outputLink, outputLink.Name,
                            ConnectionType.Link, PortDirection.In, NodeWrangler));
                        cheap.SortGraph(wrangler.Vertices.Last());
                    }
                    
                    continue;
                }

                EntityNode node = EntityNode.GetNodeFromEntity(assetObject, NodeWrangler);
                cheap.SortGraph(node);
                
                wrangler.AddVertexTransient(node);
            }

            #region Populating connections

            foreach (dynamic propertyConnection in ((dynamic)wrangler.Asset.RootObject).PropertyConnections)
            {
                PointerRef source = propertyConnection.Source;
                PointerRef target = propertyConnection.Target;

                IEntityNode sourceNode = null;
                IEntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in property connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(propertyConnection.SourceField, PortDirection.Out, ConnectionType.Property);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
                        if (sourceNode == null)
                        {
                            // Import the node
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                            sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(sourceNode);
                        
                            wrangler.AddVertexTransient(sourceNode);
                        }
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in property connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(propertyConnection.TargetField, PortDirection.In, ConnectionType.Property);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                        
                        // Import the node
                        if (targetNode == null)
                        {
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                            targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(targetNode);
                        
                            wrangler.AddVertexTransient(targetNode);
                        }
                    } break;
                }
                
                if (sourceNode.GetOutput(propertyConnection.SourceField, ConnectionType.Property) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        sourceNode.AddOutput(new PropertyOutput(propertyConnection.SourceField, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        targetNode.AddInput(new PropertyInput(propertyConnection.TargetField, targetNode));
                    });
                }

                PropertyOutput output = (PropertyOutput)sourceNode.GetOutput(propertyConnection.SourceField, ConnectionType.Property);
                PropertyInput input = (PropertyInput)targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property);
                
                wrangler.AddConnectionTransient(output, input, propertyConnection);
            }
            
            foreach (dynamic linkConnection in ((dynamic)wrangler.Asset.RootObject).LinkConnections)
            {
                PointerRef source = linkConnection.Source;
                PointerRef target = linkConnection.Target;

                IEntityNode sourceNode = null;
                IEntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in link connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(linkConnection.SourceField, PortDirection.Out, ConnectionType.Link);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
                        if (sourceNode == null)
                        {
                            // Import the node
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                            sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(sourceNode);
                        
                            wrangler.AddVertexTransient(sourceNode);
                        }
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in link connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(linkConnection.TargetField, PortDirection.In, ConnectionType.Link);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                        
                        // Import the node
                        if (targetNode == null)
                        {
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                            targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(targetNode);
                        
                            wrangler.AddVertexTransient(targetNode);
                        }
                    } break;
                }
                
                if (sourceNode.GetOutput(linkConnection.SourceField, ConnectionType.Link) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        sourceNode.AddOutput(new LinkOutput(linkConnection.SourceField, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(linkConnection.TargetField, ConnectionType.Link) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        targetNode.AddInput(new LinkInput(linkConnection.TargetField, targetNode));
                    });
                }

                LinkOutput output = (LinkOutput)sourceNode.GetOutput(linkConnection.SourceField, ConnectionType.Link);
                LinkInput input = (LinkInput)targetNode.GetInput(linkConnection.TargetField, ConnectionType.Link);
                
                wrangler.AddConnectionTransient(output, input, linkConnection);
            }
            
            foreach (dynamic eventConnection in ((dynamic)wrangler.Asset.RootObject).EventConnections)
            {
                PointerRef source = eventConnection.Source;
                PointerRef target = eventConnection.Target;

                IEntityNode sourceNode = null;
                IEntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in event connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(eventConnection.SourceEvent.Name, PortDirection.Out, ConnectionType.Event);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
                        if (sourceNode == null)
                        {
                            // Import the node
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                            sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(sourceNode);
                        
                            wrangler.AddVertexTransient(sourceNode);
                        }
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in event connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(eventConnection.TargetEvent.Name, PortDirection.In, ConnectionType.Event);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                        
                        // Import the node
                        if (targetNode == null)
                        {
                            EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                            targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                            cheap.SortGraph(targetNode);
                        
                            wrangler.AddVertexTransient(targetNode);
                        }
                    } break;
                }
                
                if (sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        sourceNode.AddOutput(new EventOutput(eventConnection.SourceEvent.Name, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event) == null)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        targetNode.AddInput(new EventInput(eventConnection.TargetEvent.Name, targetNode));
                    });
                }

                EventOutput output = (EventOutput)sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event);
                EventInput input = (EventInput)targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event);

                wrangler.AddConnectionTransient(output, input, eventConnection);
            }

            #endregion

            if (!LayoutManager.LayoutExists($"{assetEntry.Name}.lyt"))
            {
                LayoutManager.SortLayout();
            }
            else
            {
                LayoutManager.LoadLayoutRelative($"{assetEntry.Name}.lyt");
            }
        }

        protected override void NodePropertyGrid_OnOnModified(object sender, ItemModifiedEventArgs e)
        {
            base.NodePropertyGrid_OnOnModified(sender, e);
            ((ComponentNodeWrangler)NodeWrangler).UpdateComponentCount();
        }
    }
}