using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager.Sugiyama;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.CheapGraph;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Controls;
using FrostyEditor;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager
{
    /// <summary>
    /// FORMAT STRUCTURE:
    /// int - FormatVersion
    /// int - TransientCount
    ///
    /// NullTerminatedString - TypeHeader // Used to identify which Transient class to use for loading data
    /// dynamic[] - TransientData // Transient handles all data saving.
    ///
    /// int - VertexCount
    ///
    /// bool - isComplex
    /// int - type
    /// guid - fileguid
    /// assetclassguid - VertId // The index of the vert in the INodeWrangler.Nodes list
    /// Point - Location
    /// Double - SizeX
    /// Double - SizeY
    ///
    /// int - InputsCount
    ///
    /// NullTerminatedString - Name
    /// int - ConnectionType
    /// int - Realm
    /// bool - HasPlayer
    /// bool - IsInterface
    ///
    /// int - OutputsCount
    ///
    /// NullTerminatedString - Name
    /// int - ConnectionType
    /// int - Realm
    /// bool - HasPlayer
    /// bool - IsInterface
    /// </summary>
    public class EntityLayoutManager : BaseLayoutManager
    {
        public override int Version => 1006;

        public virtual bool IsValid(EbxAssetEntry assetEntry)
        {
            return true;
        }

        public override bool SaveLayout(string path)
        {
            FileInfo fileInfo = new FileInfo($@"{CurrentLayoutPath}\{path.Replace("/", "\\")}");
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            try
            {
                LayoutWriter layoutWriter =
                    new LayoutWriter(new FileStream($@"{CurrentLayoutPath}\{path.Replace("/", "\\")}",
                        FileMode.Create));
                layoutWriter.Write(Version);

                List<IVertex> verts = new List<IVertex>();
                List<ITransient> transients = new List<ITransient>();

                foreach (IVertex vertex in NodeWrangler.Vertices)
                {
                    if (vertex is ITransient transient)
                    {
                        transients.Add(transient);
                    }
                    else
                    {
                        verts.Add(vertex);
                    }
                }

                layoutWriter.Write(verts.Count);

                foreach (IVertex vertex in verts)
                {
                    if (vertex is EntityNode node)
                    {
                        layoutWriter.Write(true);
                        layoutWriter.Write((int)node.Type);
                        layoutWriter.Write(node.FileGuid);
                        layoutWriter.Write(node.InternalGuid);
                        layoutWriter.Write(vertex.Location);
                        layoutWriter.Write(vertex.Size.Width);
                        layoutWriter.Write(vertex.Size.Height);

                        layoutWriter.Write(node.Inputs.Count);
                        foreach (IPort input in node.Inputs)
                        {
                            layoutWriter.WriteNullTerminatedString(input.Name);
                            layoutWriter.Write((int)((EntityPort)input).Type);
                            layoutWriter.Write((int)((EntityPort)input).Realm);
                            layoutWriter.Write(((EntityPort)input).HasPlayer);
                            layoutWriter.Write(((EntityPort)input).IsInterface);
                        }

                        layoutWriter.Write(node.Outputs.Count);
                        foreach (IPort output in node.Outputs)
                        {
                            layoutWriter.WriteNullTerminatedString(output.Name);
                            layoutWriter.Write((int)((EntityPort)output).Type);
                            layoutWriter.Write((int)((EntityPort)output).Realm);
                            layoutWriter.Write(((EntityPort)output).HasPlayer);
                            layoutWriter.Write(((EntityPort)output).IsInterface);
                        }
                    }
                    else
                    {
                        layoutWriter.Write(false);
                        layoutWriter.Write(NodeWrangler.Vertices.IndexOf(vertex));
                        layoutWriter.Write(vertex.Location);
                        layoutWriter.Write(vertex.Size.Width);
                        layoutWriter.Write(vertex.Size.Height);
                    }
                }
                
                layoutWriter.Write(transients.Count);
                
                foreach (ITransient transient in transients)
                {
                    try
                    {
                        layoutWriter.WriteNullTerminatedString(transient.GetType().Name);
                        transient.Save(layoutWriter);
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Transient {0} failed with error {1}", transient.ToString(), e.Message);
                        App.Logger.LogError("Stacktrace: {0}", e.StackTrace);
                        App.Logger.LogWarning("Careful, layout maybe corrupted!");
                        return false;
                    }
                }

                layoutWriter.Dispose();
            }
            catch (IOException)
            {
                App.Logger.LogError("Unable to write to this file. Are you sure its not being used and is accessible?");
                return false;
            }
            catch (Exception e)
            {
                App.Logger.LogError("Unable to write to file due to exception {0}", e.Message);
                return false;
            }

            return true;
        }

        public override bool LoadLayout(string path)
        {
            if (!File.Exists(path))
                return false;

            LayoutReader layoutReader = new LayoutReader(new FileStream(path, FileMode.Open));
            if (layoutReader.ReadInt() != Version)
            {
                MessageBoxResult result = FrostyMessageBox.Show(
                    "It appears the layout file associated with this is older then the current version. Would you like me to read it anyway?", 
                    "Graph Layout Manager", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    layoutReader.Dispose();
                    return false;
                }
            }

            // Read through all the nodes
            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            int count = layoutReader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (layoutReader.ReadBoolean())
                {
                    EntityNode node;

                    if ((PointerRefType)layoutReader.ReadInt() == PointerRefType.Internal)
                    {
                        layoutReader.ReadGuid();
                        node = wrangler.GetEntityNode(layoutReader.ReadAssetClassGuid());
                    }
                    else
                    {
                        Guid fileGuid = layoutReader.ReadGuid();
                        AssetClassGuid internalGuid = layoutReader.ReadAssetClassGuid();
                        node = wrangler.GetEntityNode(fileGuid, internalGuid);
                    }

                    // SKIP!
                    if (node == null)
                    {
                        layoutReader.ReadPoint();
                        layoutReader.ReadDouble();
                        layoutReader.ReadDouble();
                        int portcount = layoutReader.ReadInt();
                        
                        // Read inputs
                        for (int j = 0; j < portcount; j++)
                        {
                            layoutReader.ReadNullTerminatedString();
                            layoutReader.ReadInt();
                            layoutReader.ReadInt();

                            layoutReader.ReadBoolean();
                            layoutReader.ReadBoolean();
                        }
                    
                        // Read outputs
                        portcount = layoutReader.ReadInt();
                        for (int j = 0; j < portcount; j++)
                        {
                            layoutReader.ReadNullTerminatedString(); 
                            layoutReader.ReadInt(); 
                            layoutReader.ReadInt();

                            layoutReader.ReadBoolean();
                            layoutReader.ReadBoolean();
                        }
                        
                        continue;
                    }
                    
                    node.Location = layoutReader.ReadPoint();
                    double width = layoutReader.ReadDouble();
                    double height = layoutReader.ReadDouble();
                    node.Size = new Size(width, height);

                    int portCount = layoutReader.ReadInt();
                    
                    // Read inputs
                    for (int j = 0; j < portCount; j++)
                    {
                        string name = layoutReader.ReadNullTerminatedString();
                        ConnectionType type = (ConnectionType)layoutReader.ReadInt();
                        Realm realm = (Realm)layoutReader.ReadInt();
                        EntityInput port = ((IEntityNode)node).GetInput(name, type);

                        if (port == null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                port = node.AddInput(name, type, realm);
                            });
                        }

                        port.HasPlayer = layoutReader.ReadBoolean();
                        port.IsInterface = layoutReader.ReadBoolean();
                    }
                    
                    // Read outputs
                    portCount = layoutReader.ReadInt();
                    for (int j = 0; j < portCount; j++)
                    {
                        string name = layoutReader.ReadNullTerminatedString();
                        ConnectionType type = (ConnectionType)layoutReader.ReadInt();
                        Realm realm = (Realm)layoutReader.ReadInt();
                        EntityOutput port = node.GetOutput(name, type);
                        
                        if (port == null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                port = node.AddOutput(name, type, realm);
                            });
                        }

                        port.HasPlayer = layoutReader.ReadBoolean();
                        port.IsInterface = layoutReader.ReadBoolean();
                    }
                }
                else
                {
                    IVertex vertex = NodeWrangler.Vertices.ElementAtOrDefault(layoutReader.ReadInt());

                    // SKIP!
                    if (vertex == null)
                    {
                        layoutReader.ReadPoint();
                        layoutReader.ReadDouble();
                        layoutReader.ReadDouble();
                        continue;
                    }
                    
                    vertex.Location = layoutReader.ReadPoint();
                    double width = layoutReader.ReadDouble();
                    double height = layoutReader.ReadDouble();
                    vertex.Size = new Size(width, height);
                }
            }
            
            // Read through all trans
            count = layoutReader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string key = layoutReader.ReadNullTerminatedString();
                if (!ExtensionsManager.TransientNodeExtensions.ContainsKey(key))
                {
                    layoutReader.Dispose();
                    App.Logger.LogError("Unable to find transient node {0}", key);
                    return false;
                }

                Type transType = ExtensionsManager.TransientNodeExtensions[key];
                try
                {
                    ITransient transient;
                    if (transType.GetConstructor(new Type[] { typeof(INodeWrangler) }) != null)
                    {
                        transient = (ITransient)Activator.CreateInstance(transType, NodeWrangler);
                    }
                    else
                    {
                        transient = (ITransient)Activator.CreateInstance(transType);
                    }

                    if (transient.Load(layoutReader))
                    {
                        NodeWrangler.AddVertex(transient);
                    }
                }
                catch (Exception e)
                {
                    App.Logger.LogError("Transient {0} failed with error {1}", transType.ToString(), e.Message);
                    App.Logger.LogError("Stacktrace: {0}", e.StackTrace);
                    layoutReader.Dispose();
                    return false;
                }
            }
            
            layoutReader.Dispose();

            return true;
        }

        public override void SortLayout(bool optimized = false)
        {
            if (optimized)
            {
                CheapMethod cheap = new CheapMethod(NodeWrangler);
                cheap.SortGraph();
                return;
            }
            
            EntitySugiyamaMethod sugiyamaMethod = new EntitySugiyamaMethod(NodeWrangler.Connections.ToList(), NodeWrangler.Vertices.ToList(), NodeWrangler);
            sugiyamaMethod.SortGraph();
        }

        public EntityLayoutManager(INodeWrangler nodeWrangler) : base(nodeWrangler)
        {
        }
    }
}