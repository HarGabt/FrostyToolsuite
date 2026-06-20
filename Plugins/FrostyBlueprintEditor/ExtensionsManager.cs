using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BlueprintEditorPlugin.Attributes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Models.Nodes;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin
{
    /// <summary>
    /// Central class for management of Blueprint Editor extensions.
    /// This handles everything, from GraphEditors to Node mappings.
    /// </summary>
    public static class ExtensionsManager
    {
        public static readonly Dictionary<string, Type> EntityNodeExtensions = new();
        public static readonly Dictionary<string, Type> TransientNodeExtensions = new();
        
        private static List<Type> _graphEditors = new();
        public static IEnumerable<Type> GraphEditorExtensions => _graphEditors;

        private static List<Type> _blueprintMenuItems = new();
        public static IEnumerable<Type> BlueprintMenuItemExtensions => _blueprintMenuItems;
        
        private static List<Type> _entityLayoutManagers = new();
        public static IEnumerable<Type> EntityLayoutManagers => _entityLayoutManagers;

        /// <summary>
        /// Initiates the ExtensionManager
        /// </summary>
        public static void Initiate()
        {
            // Register internal Entity Nodes
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(EntityNode)) && !type.IsAbstract && type.Name != "EntityMappingNode")
                {
                    try
                    {
                        EntityNode node = (EntityNode)Activator.CreateInstance(type);
                        if (node.ObjectType == null)
                        {
                            App.Logger.LogError("Object Type for node {0} was not specified.", type.Name);
                        }
                        if (node.IsValid() && !EntityNodeExtensions.ContainsKey(node.ObjectType))
                        {
                            EntityNodeExtensions.Add(node.ObjectType, type);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{type.Name} failed!");
                        App.Logger.LogError("Entity node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
                if (type.GetInterface("ITransient") != null && !type.IsAbstract)
                {
                    try
                    {
                        ITransient trans = (ITransient)Activator.CreateInstance(type);
                        if (trans.IsValid() && !TransientNodeExtensions.ContainsKey(type.Name))
                        {
                            TransientNodeExtensions.Add(type.Name, type);
                        }
                        else if (TransientNodeExtensions.ContainsKey(type.Name))
                        {
                            App.Logger.LogError("To whomever may read this: Please give type {0} a unique name. Transient nodes must have unique type names god dammit!", type.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Transient node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
                if (type.GetInterface("IEbxGraphEditor") != null && !type.IsAbstract)
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IEbxGraphEditor graphEditor = (IEbxGraphEditor)Activator.CreateInstance(type);
                            if (graphEditor.IsValid())
                            {
                                _graphEditors.Add(type);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Graph Editor {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
            }
            
            foreach (string item in Directory.EnumerateFiles("Plugins", "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new(item);
                    Assembly plugin = Assembly.LoadFile(fileInfo.FullName);

                    foreach (Attribute attribute in plugin.GetCustomAttributes())
                    {
                        if (attribute is RegisterEntityNode entityRegister)
                        {
                            RegisterExtension(entityRegister);
                        }
                        else if (attribute is RegisterEbxGraphEditor graphRegister)
                        {
                            RegisterExtension(graphRegister);
                        }
                        else if (attribute is RegisterBlueprintMenuExtension menuExtension)
                        {
                            RegisterExtension(menuExtension);
                        }
                        else if (attribute is RegisterEntityLayoutExtension layoutExtension)
                        {
                            RegisterExtension(layoutExtension);
                        }
                    }
                }
                catch (Exception )
                {
                    // Suppress all errors please!
                }
            }
        }

        #region Register methods

        public static void RegisterExtension(RegisterEbxGraphEditor graphRegister)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var extension = (IEbxGraphEditor)Activator.CreateInstance(graphRegister.GraphType);

                    if (extension.IsValid() && extension is Control)
                    {
                        // Override our internal extension with external one
                        _graphEditors.Add(graphRegister.GraphType);
                    }
                    else if (extension is not Control)
                    {
                        App.Logger.LogError("Graph editor {0} must be a control", graphRegister.GraphType.Name);
                    }
                }
                catch (Exception)
                {
                    App.Logger.LogError("Could not load graph extension {0}", graphRegister.GraphType.Name);
                }
            });
        }

        public static void RegisterExtension(RegisterEntityNode entityRegister)
        {
            try
            {
                var extension = (EntityNode)Activator.CreateInstance(entityRegister.EntityNodeExtension);
                if (extension.IsValid())
                {
                    // Override our internal extension with external one
                    if (EntityNodeExtensions.ContainsKey(extension.ObjectType))
                    {
                        EntityNodeExtensions.Remove(extension.ObjectType);
                    }

                    EntityNodeExtensions.Add(extension.ObjectType, entityRegister.EntityNodeExtension);
                }
            }
            catch (Exception)
            {
                App.Logger.LogError("Could not load node extension {0}", entityRegister.EntityNodeExtension.Name);
            }
        }

        public static void RegisterExtension(RegisterBlueprintMenuExtension menuRegister)
        {
            try
            {
                var extension = (BlueprintMenuItemExtension)Activator.CreateInstance(menuRegister.MenuType);
                if (extension.IsValid())
                {
                    _blueprintMenuItems.Add(menuRegister.MenuType);
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError("Blueprint editor menu extension {0} threw the exception {1} at {2}", menuRegister.MenuType.Name, e.Message, e.StackTrace);
            }
        }

        public static void RegisterExtension(RegisterEntityLayoutExtension layoutExtension)
        {
            try
            {
                var extension = (EntityLayoutManager)Activator.CreateInstance(layoutExtension.LayoutManagerType, new object[] { null });
                if (extension.IsValid())
                {
                    _entityLayoutManagers.Add(layoutExtension.LayoutManagerType);
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError("Blueprint editor menu extension {0} threw the exception {1} at {2}", layoutExtension.LayoutManagerType.Name, e.Message, e.StackTrace);
            }
        }

        #endregion

        /// <summary>
        /// Gets a valid <see cref="IEbxGraphEditor"/> for the specified <see cref="EbxAssetEntry"/>
        /// </summary>
        /// <param name="assetEntry"></param>
        /// <returns></returns>
        public static IEbxGraphEditor GetValidGraphEditor(EbxAssetEntry assetEntry)
        {
            foreach (Type graphType in GraphEditorExtensions)
            {
                IEbxGraphEditor graphEditor = (IEbxGraphEditor)Activator.CreateInstance(graphType);
                if (graphEditor.IsValid(assetEntry))
                {
                    return graphEditor;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Gets a valid <see cref="EntityLayoutManager"/> for the specified <see cref="EbxAssetEntry"/>
        /// </summary>
        /// <param name="assetEntry"></param>
        /// <returns></returns>
        public static EntityLayoutManager GetValidLayoutManager(EbxAssetEntry assetEntry)
        {
            foreach (Type layoutType in _entityLayoutManagers)
            {
                EntityLayoutManager layoutManager = (EntityLayoutManager)Activator.CreateInstance(layoutType, new object[] { null });
                if (layoutManager.IsValid(assetEntry))
                {
                    return layoutManager;
                }
            }

            return null;
        }
    }
}