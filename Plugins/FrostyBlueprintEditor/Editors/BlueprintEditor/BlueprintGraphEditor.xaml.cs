using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.CheapGraph;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Options;
using BlueprintEditorPlugin.Views.Editor;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;
using Prism.Commands;
using App = FrostyEditor.App;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor
{
    /// <summary>
    /// A <see cref="IEbxGraphEditor"/> for editing Blueprints, such as LogicPrefabs or SpatialPrefabs.
    ///
    /// <seealso cref="EntityNode"/>
    /// <seealso cref="EntityConnection"/>
    /// <seealso cref="EntityNodeWrangler"/>
    /// <seealso cref="EntityLayoutManager"/>
    /// </summary>
    public partial class BlueprintGraphEditor : UserControl, IEbxGraphEditor
    {
        #region Graph Editor Implementation

        public INodeWrangler NodeWrangler { get; set; }
        public ILayoutManager LayoutManager { get; set; }

        public virtual bool IsValid()
        {
            return true;
        }

        public virtual bool IsValid(EbxAssetEntry assetEntry)
        {
            EbxAsset asset = App.AssetManager.GetEbx(assetEntry);
            Type assetType = asset.RootObject.GetType();
            
            // We only check property connections since we can assume if it has property connections it has everything else
            // We also don't want this to have an Object, just objects. BaseComponentGraphEditor handles graphs with components
            if (assetType.GetProperty("Objects") != null 
                && assetType.GetProperty("PropertyConnections") != null 
                && assetType.GetProperty("Interface") != null
                && assetType.GetProperty("Object") == null)
            {
                return true;
            }
            
            return false;
        }

        public virtual void Closed()
        {
            if (!EditorOptions.SaveOnExit) return;
            
            EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
            LayoutManager.SaveLayout($"{assetEntry.Name}.lyt");
        }

        #endregion

        public BlueprintGraphEditor()
        {
            NodeWrangler = new EntityNodeWrangler();
            
            InitializeComponent();
            NodePropertyGrid.NodeWrangler = NodeWrangler;
            
            LayoutManager = new EntityLayoutManager(NodeWrangler);

            foreach (Type extensionType in ExtensionsManager.BlueprintMenuItemExtensions)
            {
                BlueprintMenuItemExtension menuExtension =
                    (BlueprintMenuItemExtension)Activator.CreateInstance(extensionType);
                menuExtension.GraphEditor = this;

                MenuItem subItem = new MenuItem
                {
                    Header = menuExtension.DisplayName,
                    ToolTip = menuExtension.ToolTip,
                    Icon = new Image {Source = menuExtension.Icon},
                    Command = menuExtension.ButtonClicked
                };

                if (menuExtension.SubLevelMenuName != null)
                {
                    MenuItem topItem = null;
                    foreach (MenuItem menuItem in MenuItems.Items)
                    {
                        if ((string)menuItem.Header == menuExtension.SubLevelMenuName)
                        {
                            topItem = menuItem;
                        }
                    }


                    if (topItem == null)
                    {
                        topItem = new MenuItem()
                        {
                            Header = menuExtension.SubLevelMenuName
                        };
                        MenuItems.Items.Add(topItem);
                    }
                    
                    topItem.Items.Add(subItem);
                }
                else
                {
                    MenuItems.Items.Add(subItem);
                }
            }
        }

        public virtual void LoadAsset(EbxAssetEntry assetEntry)
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
                        if (source.Internal == null)
                        {
                            App.Logger.LogError("Reference in connection was invalid");
                            continue;
                        }
                        
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(propertyConnection.SourceField, PortDirection.Out, ConnectionType.Property);
                            if (sourceNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", propertyConnection.SourceField.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(source.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                                return;
                            }
                            
                            sourceNode = EntityNode.GetNodeFromEntity(obj, source.External.FileGuid, NodeWrangler);
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
                        if (target.Internal == null)
                        {
                            App.Logger.LogError("Reference in connection was invalid");
                            continue;
                        }
                        
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(propertyConnection.TargetField, PortDirection.In, ConnectionType.Property);
                            if (targetNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", propertyConnection.TargetField.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(target.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                            }
                            
                            targetNode = EntityNode.GetNodeFromEntity(obj, target.External.FileGuid, NodeWrangler);
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
                            if (sourceNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", linkConnection.SourceField.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(source.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                                return;
                            }
                            
                            sourceNode = EntityNode.GetNodeFromEntity(obj, source.External.FileGuid, NodeWrangler);
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
                            if (targetNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", linkConnection.TargetField.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(target.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                                return;
                            }
                            
                            targetNode = EntityNode.GetNodeFromEntity(obj, target.External.FileGuid, NodeWrangler);
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
                            if (sourceNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", eventConnection.SourceEvent.Name.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(source.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                                return;
                            }
                            
                            sourceNode = EntityNode.GetNodeFromEntity(obj, source.External.FileGuid, NodeWrangler);
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
                            if (targetNode == null)
                            {
                                App.Logger.LogError("Unable to find an interface entry by the name of {0}", eventConnection.TargetEvent.ToString());
                                continue;
                            }
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
                            object obj = asset.GetObject(target.External.ClassGuid);
                            if (obj == null)
                            {
                                App.Logger.LogError("Reference in connection was invalid");
                                return;
                            }
                            
                            targetNode = EntityNode.GetNodeFromEntity(obj, target.External.FileGuid, NodeWrangler);
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

        #region Static

        public static List<Type> Types = new List<Type>();
        public static List<IVertex> NodeUtils = new List<IVertex>();

        static BlueprintGraphEditor()
        {
            NodifyEditor.EnableRenderingContainersOptimizations = true;
            NodifyEditor.OptimizeRenderingMinimumContainers = 10;
            NodifyEditor.OptimizeRenderingZoomOutPercent = 0.1;
            
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                Types.Add(type);
            }
            
            NodeUtils.Add(new EntityComment("Comment"));
        }

        #endregion

        #region Nodes

        #region Visuals

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object addedItem in e.AddedItems)
            {
                ((IVertex)addedItem).IsSelected = true;
            }

            foreach (object removedItem in e.RemovedItems)
            {
                ((IVertex)removedItem).IsSelected = false;
            }

            if (e.AddedItems.Count != 0)
            {
                switch (e.AddedItems[0])
                {
                    case InterfaceNode interfaceNode:
                    {
                        NodePropertyGrid.Object = interfaceNode.EditArgs;
                    } break;
                    case IObjectContainer container:
                    {
                        NodePropertyGrid.Object = container.Object;
                    } break;
                }
            }
            else
            {
                NodePropertyGrid.Object = new object();
            }
        }
        
        #endregion

        protected virtual void NodePropertyGrid_OnOnModified(object sender, ItemModifiedEventArgs e)
        {
            // If the user holds down alt that means all selected nodes should have their properties set the same
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                FrostyTaskWindow.Show("Updating properties...", "", task =>
                {
                    foreach (IVertex selectedNode in NodeWrangler.SelectedVertices)
                    {
                        switch (selectedNode)
                        {
                            case IEntityNode node:
                            {
                                node.TrySetProperty(e.Item.Name, e.NewValue);
                                node.OnObjectModified(sender, e);
                                ((IEbxNodeWrangler)NodeWrangler).ModifyAsset();
                            } break;
                            case EntityComment comment:
                            {
                                comment.OnObjectModified(sender, e);
                            } break;
                            case IObjectContainer container:
                            {
                                container.OnObjectModified(sender, e);
                            } break;
                        }
                    }
                });
            }
            else
            {
                switch (NodeWrangler.SelectedVertices[0])
                {
                    case EntityNode node:
                    {
                        node.OnObjectModified(sender, e);
                        ((EntityNodeWrangler)NodeWrangler).ModifyAsset();
                    } break;
                    case InterfaceNode interfaceNode:
                    {
                        interfaceNode.OnObjectModified(sender, e);
                        ((EntityNodeWrangler)NodeWrangler).ModifyAsset();
                    } break;
                    case EntityComment comment:
                    {
                        comment.OnObjectModified(sender, e);
                    } break;
                    case IObjectContainer container:
                    {
                        container.OnObjectModified(sender, e);
                    } break;
                }
            }
        }
        
        #region Adding & removing nodes
        
        protected virtual void DeleteNode_OnClick(object sender, RoutedEventArgs e)
        {
            List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedVertices);
            foreach (IVertex selectedNode in oldSelection)
            {
                if (selectedNode is IRedirect redirect)
                {
                    if (redirect.SourceRedirect != null)
                    {
                        NodeWrangler.RemoveVertex(redirect.SourceRedirect);
                    }
                    else
                    {
                        NodeWrangler.RemoveVertex(redirect.TargetRedirect);
                    }
                }
                
                NodeWrangler.RemoveVertex(selectedNode);
            }
        }
        
        protected virtual void DuplicateNode_OnClick(object sender, RoutedEventArgs e)
        {
            List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedVertices);
            NodeWrangler.SelectedVertices.Clear();
            
            foreach (IVertex selectedNode in oldSelection)
            {
                if (selectedNode is EntityNode entityNode)
                {
                    FrostyClipboard.Current.SetData(entityNode.Object); // TODO: Work around, need to copy data
                    EntityNode newNode = EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                    newNode.Location = new Point(selectedNode.Location.X + 15, selectedNode.Location.Y + 15);
                    NodeWrangler.AddVertex(newNode);
                    NodeWrangler.SelectedVertices.Add(newNode);
                }

                if (selectedNode is InterfaceNode)
                {
                    App.Logger.LogError("Cannot duplicate interface nodes.");
                }
            }
        }
        
        private void AddInterface_OnClick(object sender, RoutedEventArgs e)
        {
            AddInterfaceArgs args = new AddInterfaceArgs();
            MessageBoxResult result = EditPromptWindow.Show(args, "Add Interface");
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
                
            if (wrangler.GetInterfaceNode(args.Name, args.Direction, args.Type) != null)
            {
                App.Logger.LogError("Cannot have duplicate interface nodes");
                return;
            }
                
            // Ensure that the interface exists
            PointerRef interfaceRef = ((dynamic)wrangler.Asset.RootObject).Interface;
            if (interfaceRef.Type == PointerRefType.Null)
            {
                dynamic intrfc = TypeLibrary.CreateObject("InterfaceDescriptorData");
                wrangler.Asset.AddObject(intrfc);
                interfaceRef = new PointerRef(intrfc);
                ((dynamic)wrangler.Asset.RootObject).Interface = interfaceRef;
            }
                
            switch (args.Type)
            {
                case ConnectionType.Event:
                {
                    dynamic subObj = TypeLibrary.CreateObject("DynamicEvent");
                    subObj.Name = new CString(args.Name);

                    wrangler.AddVertex(new InterfaceNode(interfaceRef.Internal, subObj, args.Name, ConnectionType.Event, args.Direction, NodeWrangler)
                    {
                        SubObject = subObj,
                        // TODO: This is kind of a cheaty way of doing this, it'd be better to place at the center of the screen
                        Location = new Point(Editor.MouseLocation.X, Editor.MouseLocation.Y)
                    });
                } break;
                case ConnectionType.Link:
                {
                    dynamic subObj = TypeLibrary.CreateObject("DynamicLink");
                    subObj.Name = new CString(args.Name);

                    wrangler.AddVertex(new InterfaceNode(interfaceRef.Internal, subObj, args.Name, ConnectionType.Link, args.Direction, NodeWrangler)
                    {
                        SubObject = subObj,
                        // TODO: This is kind of a cheaty way of doing this, it'd be better to place at the center of the screen
                        Location = new Point(Editor.MouseLocation.X, Editor.MouseLocation.Y)
                    });
                } break;
                case ConnectionType.Property:
                {
                    dynamic subObj = TypeLibrary.CreateObject("DataField");
                    subObj.Name = new CString(args.Name);

                    if (args.Direction == PortDirection.Out)
                    {
                        Type enumType = subObj.AccessType.GetType();
                        subObj.AccessType = (dynamic)Enum.Parse(enumType, "FieldAccessType_Target");
                    }

                    wrangler.AddVertex(new InterfaceNode(interfaceRef.Internal, subObj, args.Name, ConnectionType.Property, args.Direction, NodeWrangler)
                    {
                        SubObject = subObj,
                        // TODO: This is kind of a cheaty way of doing this, it'd be better to place at the center of the screen
                        Location = new Point(Editor.MouseLocation.X, Editor.MouseLocation.Y)
                    });
                } break;
            }
        }

        #endregion
        
        private void PasteObject(object sender, RoutedEventArgs e)
        {
            // If there is no data to paste, clipboard crashes sometimes(dunno why)
            try
            {
                // Item is not valid for pasting
                if (FrostyClipboard.Current.GetData() == null)
                    return;

                if (!TypeLibrary.IsSubClassOf(FrostyClipboard.Current.GetData(), "GameDataContainer"))
                    return;

                EntityNode node =
                    EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                node.Location = Editor.MouseLocation;

                NodeWrangler.AddVertex(node);
                NodeWrangler.SelectedVertices.Add(node);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion

        #region Class Selector

        private void ClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (ClassList.SelectedType == null)
                return;
            
            EntityNode node = EntityNode.GetNodeFromEntity(ClassList.SelectedType, NodeWrangler);
            node.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
            NodeWrangler.AddVertex(node);
        }

        private void TransClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (TransTypeList.SelectedItem == null)
                return;
            
            Type type = TransTypeList.SelectedItem.GetType();
            IVertex vertex = (IVertex)Activator.CreateInstance(type);
            vertex.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
            
            NodeWrangler.AddVertex(vertex);
        }
        
        private void SearchForClass_Click(object sender, RoutedEventArgs e)
        {
            ClassSelector classSelector = new ClassSelector(Types.ToArray());
            if (classSelector.ShowDialog() == true)
            {
                if (classSelector.SelectedClass == null)
                    return;
                
                EntityNode node = EntityNode.GetNodeFromEntity(classSelector.SelectedClass, NodeWrangler);
                node.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
                NodeWrangler.AddVertex(node);
            }
        }
        
        private void ClassList_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ClassList.SelectedType == null)
            {
                EntDocBoxHeader.Text = "";
                EntDocBoxText.Text = "";
                return;
            }
            
            EntDocBoxHeader.Text = ClassList.SelectedType.Name;
            if (ExtensionsManager.EntityNodeExtensions.ContainsKey(ClassList.SelectedType.Name))
            {
                EntityNode node = (EntityNode)Activator.CreateInstance(ExtensionsManager.EntityNodeExtensions[ClassList.SelectedType.Name]);
                EntDocBoxText.Text = node.ToolTip;
            }
        }

        #endregion

        #region Layouts

        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Use default Sugiyama instead to prevent issues
            SugiyamaMethod sugiyamaMethod = new SugiyamaMethod(NodeWrangler.Connections.ToList(), NodeWrangler.Vertices.ToList());
            sugiyamaMethod.SortGraph();
        }

        private void SaveOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
            if (LayoutManager.SaveLayout($"{assetEntry.Name}.lyt"))
            {
                App.Logger.Log("Layout saved!");
            }
            else
            {
                App.Logger.LogError("Failed to save layout... Sorry!");
            }
        }
        
        private void ImportOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            FrostyOpenFileDialog ofd = new FrostyOpenFileDialog("Open Layout", "Blueprint Layout (*.lyt)|*.lyt|Text File (*.txt)|*.txt", "BlueprintLayout");
            if (!ofd.ShowDialog())
                return;

            LayoutManager.LoadLayout(ofd.FileName);
        }

        #endregion

        #region Visibility

        private void ControlsMenuVisible_OnClick(object sender, RoutedEventArgs e)
        {
            BlueprintEditorControlsWindow controlsWindow = new BlueprintEditorControlsWindow();
            controlsWindow.Show();
        }
        
        private void ToolboxVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (ToolboxPanel.Visibility == Visibility.Visible)
            {
                ToolboxPanel.Visibility = Visibility.Collapsed;
                ToolboxCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                ToolboxPanel.Visibility = Visibility.Visible;
                ToolboxCollum.Width = new GridLength(225, GridUnitType.Pixel);
            }
        }
        
        private void PropertyGridVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (PropertyPanel.Visibility == Visibility.Visible)
            {
                PropertyPanel.Visibility = Visibility.Collapsed;
                PropertyCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                PropertyPanel.Visibility = Visibility.Visible;
                PropertyCollum.Width = new GridLength(425, GridUnitType.Pixel);
            }
        }

        #endregion

        #region Controls

        private void Editor_OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                {
                    while (NodeWrangler.SelectedVertices.Count != 0)
                    {
                        NodeWrangler.RemoveVertex(NodeWrangler.SelectedVertices[0]);
                    }
                } break;
                case Key.D when (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                {
                    List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedVertices);
                    NodeWrangler.SelectedVertices.Clear();
            
                    foreach (IVertex selectedNode in oldSelection)
                    {
                        if (selectedNode is EntityNode entityNode)
                        {
                            FrostyClipboard.Current.SetData(entityNode.Object); // TODO: Work around, need to copy data
                            EntityNode newNode = EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                            newNode.Location = new Point(selectedNode.Location.X + 15, selectedNode.Location.Y + 15);
                            NodeWrangler.AddVertex(newNode);
                            NodeWrangler.SelectedVertices.Add(newNode);
                        }

                        if (selectedNode is InterfaceNode)
                        {
                            App.Logger.LogError("Cannot duplicate interface nodes.");
                        }
                    }
                } break;
                case Key.S when (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                {
                    EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
                    LayoutManager.SaveLayout($"{assetEntry.Name}.lyt");
                } break;
                case Key.C when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                {
                    if (NodeWrangler.SelectedVertices.Count == 0)
                        return;

                    if (NodeWrangler.SelectedVertices.LastOrDefault() is EntityNode node)
                    {
                        node.Copy();
                    }
                } break;
                case Key.X when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                {
                    if (NodeWrangler.SelectedVertices.Count == 0)
                        return;

                    if (NodeWrangler.SelectedVertices.LastOrDefault() is EntityNode node)
                    {
                        node.Copy();
                        NodeWrangler.RemoveVertex(node);
                    }
                } break;
                case Key.V when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                {
                    // If there is no data to paste, clipboard crashes sometimes(dunno why)
                    try
                    {
                        // Item is not valid for pasting
                        if (FrostyClipboard.Current.GetData() == null)
                            return;

                        if (!TypeLibrary.IsSubClassOf(FrostyClipboard.Current.GetData(), "GameDataContainer"))
                            return;

                        EntityNode node =
                            EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                        node.Location = Editor.MouseLocation;

                        NodeWrangler.AddVertex(node);
                        NodeWrangler.SelectedVertices.Add(node);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                } break;
                default:
                {
                    List<BlueprintEditorControl> controls = Config.Get("BlueprintEditorControls", new List<BlueprintEditorControl>(), ConfigScope.Game);
                    foreach (BlueprintEditorControl control in controls)
                    {
                        if (e.Key != control.Key)
                            continue;
                        
                        if (control.UseModifierKey)
                        {
                            if ((Keyboard.Modifiers & control.ModifierKey) != control.ModifierKey)
                                continue;
                        }

                        if (ExtensionsManager.TransientNodeExtensions.ContainsKey(control.TypeName))
                        {
                            IVertex vertex = (IVertex)Activator.CreateInstance(ExtensionsManager.TransientNodeExtensions[control.TypeName]);
                            vertex.Location = Editor.MouseLocation;
                            NodeWrangler.AddVertex(vertex);
                            break;
                        }

                        object obj = TypeLibrary.CreateObject(control.TypeName);
                        if (obj == null)
                        {
                            App.Logger.LogError("The control \"{0}\" is not valid, specified node not found", control.ToString());
                        }
                        
                        EntityNode node = EntityNode.GetNodeFromEntity(obj, NodeWrangler, true);
                        node.Location = Editor.MouseLocation;
                        NodeWrangler.AddVertex(node);
                        
                        break;
                    }
                } break;
            }
        }

        private void Editor_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (ToolboxTabControl.SelectedIndex == 0)
                {
                    if (ClassList.SelectedType == null)
                        return;
                    
                    EntityNode node = EntityNode.GetNodeFromEntity(ClassList.SelectedType, NodeWrangler);
                    node.Location = Editor.MouseLocation;
                    NodeWrangler.AddVertex(node);
                }
                else
                {
                    if (TransTypeList.SelectedItem == null)
                        return;
            
                    Type type = TransTypeList.SelectedItem.GetType();
                    IVertex vertex = (IVertex)Activator.CreateInstance(type);
                    vertex.Location = Editor.MouseLocation;
            
                    NodeWrangler.AddVertex(vertex);
                }
            }
        }

        #endregion

        #region Node filtering

        private void FilterBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FilterBox_Search();
        }

        private void FilterBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FilterBox_Search();
            }
        }

        private void FilterBox_Search()
        {
            if (string.IsNullOrEmpty(FilterBox.Text))
            {
                for (var index = 0; index < NodesList.Items.Count; index++)
                {
                    ListBoxItem item = NodesList.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                
                    if (item == null)
                        continue;

                    item.Visibility = Visibility.Visible;
                }
                
                return;
            }

            for (var index = 0; index < NodeWrangler.Vertices.Count; index++)
            {
                var vert = (IVertex)NodesList.Items.GetItemAt(index);
                ListBoxItem item = NodesList.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                
                if (item == null)
                    continue;

                // Parse text arguments
                if (FilterBox.Text.Contains(':'))
                {
                    string[] args = FilterBox.Text.Split(' ');
                    item.Visibility = Visibility.Visible;
                    
                    foreach (string arg in args)
                    {
                        try
                        {
                            if (item.Visibility == Visibility.Collapsed)
                                break;
                        
                            string p1 = arg.Split(':')[0].Trim();
                            string p2 = arg.Split(':')[1].Trim();

                            switch (p1)
                            {
                                case "guid":
                                {
                                    if (!(vert is IEntityObject entityObject))
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    if (entityObject.InternalGuid.ToString() != p2)
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    item.Visibility = Visibility.Visible;
                                } break;
                                case "fguid":
                                {
                                    if (!(vert is IEntityObject entityObject))
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    if (entityObject.FileGuid.ToString() == p2)
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    item.Visibility = Visibility.Visible;
                                } break;
                                case "search":
                                {
                                    if (!(vert.ToString().IndexOf(p2.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                    }
                                    else
                                    {
                                        item.Visibility = Visibility.Visible;
                                    }
                                } break;
                                case "hasproperty":
                                {
                                    if (!(vert is IEntityNode entityNode))
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }
                                
                                    if (entityNode.TryGetProperty(p2) != null)
                                    {
                                        item.Visibility = Visibility.Visible;
                                        break;
                                    }

                                    item.Visibility = Visibility.Collapsed;
                                } break;
                                case "hasvalue":
                                {
                                    string c1 = p2.Split(',')[0].Trim();
                                    string c2 = p2.Split(',')[1].Trim();
                                
                                    if (!(vert is IEntityNode entityNode))
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    object value = entityNode.TryGetProperty(c1);
                                    if (value == null)
                                    {
                                        item.Visibility = Visibility.Collapsed;
                                        break;
                                    }

                                    if (value.ToString().ToLower() == c2.ToLower())
                                    {
                                        item.Visibility = Visibility.Visible;
                                        break;
                                    }

                                    item.Visibility = Visibility.Collapsed;
                                } break;
                            }
                        }
                        catch (Exception e)
                        {
                            App.Logger.LogError("Invalid search input");
                        }
                    }
                }
                else
                {
                    if (!(vert.ToString().IndexOf(FilterBox.Text.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void NodesList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NodesList.SelectedItem is IVertex vertex)
            {
                // We want to get the center point of the node
                Point location = new Point(vertex.Location.X + (vertex.Size.Width / 2),
                    vertex.Location.Y + (vertex.Size.Height / 2));
                Editor.BringIntoView(location);
            }
        }

        #endregion

        #region Navigation

        private void RedirectGoToSource_OnClick(object sender, RoutedEventArgs e)
        {
            // the data context of the menu item will be the redirect... hopefully
            if (((MenuItem)sender).DataContext is IRedirect redirect)
            {
                if (redirect.SourceRedirect == null)
                {
                    // We want to get the center point of the node
                    Point location = new Point(redirect.TargetRedirect.Location.X + (redirect.TargetRedirect.Size.Width / 2),
                        redirect.TargetRedirect.Location.Y + (redirect.TargetRedirect.Size.Height / 2));
                    Editor.BringIntoView(location);
                }
                else
                {
                    // We want to get the center point of the node
                    Point location = new Point(redirect.SourceRedirect.Location.X + (redirect.SourceRedirect.Size.Width / 2),
                        redirect.SourceRedirect.Location.Y + (redirect.SourceRedirect.Size.Height / 2));
                    Editor.BringIntoView(location);
                }
            }
        }

        private void ConnectionGoToSource_OnClick(object sender, RoutedEventArgs e)
        {
            // the data context of the menu item will be the connection... hopefully
            if (((MenuItem)sender).DataContext is IConnection connection)
            {
                // We want to get the center point of the node
                Point location = new Point(connection.Source.Node.Location.X + (connection.Source.Node.Size.Width / 2),
                    connection.Source.Node.Location.Y + (connection.Source.Node.Size.Height / 2));
                Editor.BringIntoView(location);
            }
        }

        private void ConnectionGoToTarget_OnClick(object sender, RoutedEventArgs e)
        {
            // the data context of the menu item will be the connection... hopefully
            if (((MenuItem)sender).DataContext is IConnection connection)
            {
                // We want to get the center point of the node
                Point location = new Point(connection.Target.Node.Location.X + (connection.Target.Node.Size.Width / 2),
                    connection.Target.Node.Location.Y + (connection.Target.Node.Size.Height / 2));
                Editor.BringIntoView(location);
            }
        }

        #endregion
    }
}