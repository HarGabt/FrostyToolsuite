using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostyEditor;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ObjectReference
{
    public abstract class BaseLogicRefEntity : BaseReferenceEntityData
    {
        protected override void UpdateRef()
        {
            PointerRef pointer = (PointerRef)TryGetProperty("Blueprint");
            if (pointer == null)
            {
                App.Logger.LogError("Could not get blueprint for {0}!", ToString());
            }

            if (Inputs.Count > 1 || Outputs.Count != 0)
            {
                List<IPort> inputs = Inputs.ToList();
                foreach (EntityInput input in inputs)
                {
                    // Only remove interfaces
                    if (!input.IsInterface)
                        continue;
                    
                    RemoveInput(input);
                }
                
                Outputs.Clear();
            }

            if (pointer.Type == PointerRefType.Internal || pointer.Type == PointerRefType.Null)
            {
                Header = $"{ObjectType} (invalid ref)";

                return;
            }

            EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(pointer.External.FileGuid);
            EbxAsset asset = App.AssetManager.GetEbx(assetEntry);

            Header = $"{ObjectType} ({assetEntry.Filename})";

            if (asset.RootObject.GetType().GetProperty("PropertyConnections") == null)
            {
                return;
            }

            PointerRef interfaceRef = ((dynamic)asset.RootObject).Interface;
            if (interfaceRef.Type == PointerRefType.Null)
            {
                return;
            }
            
            AssetClassGuid interfaceGuid = ((dynamic)interfaceRef.Internal).GetInstanceGuid();

            foreach (dynamic field in ((dynamic)interfaceRef.Internal).Fields)
            {
                switch (field.AccessType.ToString())
                {
                    case "FieldAccessType_Source":
                    {
                        AddOutput(new PropertyOutput(field.Name.ToString(), this)
                        {
                            Realm = Realm.Any
                        });
                    } break;
                    case "FieldAccessType_Target":
                    {
                        AddInput(new PropertyInput(field.Name.ToString(), this)
                        {
                            IsInterface = true,
                            Realm = Realm.Any
                        });
                    } break;
                    case "FieldAccessType_SourceAndTarget":
                    {
                        AddOutput(new PropertyOutput(field.Name.ToString(), this)
                        {
                            Realm = Realm.Any
                        });
                        AddInput(new PropertyInput(field.Name.ToString(), this)
                        {
                            IsInterface = true,
                            Realm = Realm.Any
                        });
                    } break;
                }
            }

            foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
            {
                AddInput(inputEvent.Name.ToString(), ConnectionType.Event);
            }
            
            foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
            {
                AddOutput(outputEvent.Name.ToString(), ConnectionType.Event);
            }
            
            foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
            {
                AddInput(inputLink.Name.ToString(), ConnectionType.Link);
            }
            
            foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
            {
                AddOutput(outputLink.Name.ToString(), ConnectionType.Link);
            }

            // TODO: THIS FUCKING SUUUUCKS!!!!
            // We are enumerating over potentially thousands of connections for the realm...
            foreach (dynamic propertyConnection in ((dynamic)asset.RootObject).PropertyConnections)
            {
                PointerRef source = propertyConnection.Source;
                PointerRef target = propertyConnection.Target;
                
                if (source.Type != PointerRefType.Internal || target.Type != PointerRefType.Internal)
                    continue;
                
                if (((dynamic)source.Internal).GetInstanceGuid() != interfaceGuid && ((dynamic)target.Internal).GetInstanceGuid() != interfaceGuid)
                    continue;

                if (((dynamic)source.Internal).GetInstanceGuid() == interfaceGuid)
                {
                    PropertyInput input = GetInput(propertyConnection.SourceField.ToString(), ConnectionType.Property);
                    if (input == null)
                        continue;
                    
                    input.Realm = PropertyFlagsHelper.GetAsRealm((uint)propertyConnection.Flags);
                }
                
                if (((dynamic)target.Internal).GetInstanceGuid() == interfaceGuid)
                {
                    PropertyOutput output = GetOutput(propertyConnection.TargetField.ToString(), ConnectionType.Property);
                    if (output == null)
                        continue;

                    output.Realm = PropertyFlagsHelper.GetAsRealm((uint)propertyConnection.Flags);
                }
            }
            
            foreach (dynamic eventConnection in ((dynamic)asset.RootObject).EventConnections)
            {
                PointerRef source = eventConnection.Source;
                PointerRef target = eventConnection.Target;
                
                if (source.Type != PointerRefType.Internal || target.Type != PointerRefType.Internal)
                    continue;
                
                if (((dynamic)source.Internal).GetInstanceGuid() != interfaceGuid && ((dynamic)target.Internal).GetInstanceGuid() != interfaceGuid)
                    continue;

                if (((dynamic)source.Internal).GetInstanceGuid() == interfaceGuid)
                {
                    EventInput input = GetInput(eventConnection.SourceEvent.Name.ToString(), ConnectionType.Event);
                    if (input == null)
                    {
                        continue;
                    }

                    input.Realm = input.ParseRealm(eventConnection.TargetType.ToString());
                }
                
                if (((dynamic)target.Internal).GetInstanceGuid() == interfaceGuid)
                {
                    EventOutput output = GetOutput(eventConnection.TargetEvent.Name.ToString(), ConnectionType.Event);
                    if (output == null)
                    {
                        continue;
                    }
                    
                    // TODO: Stupid. Stupid. Stupid. Stupid. Stupid. Stupud. Stupuid. Stupid. Stupid. Stpid. Stupid. Stupid. Stupid
                    if (ExtensionsManager.EntityNodeExtensions.ContainsKey(source.Internal.GetType().Name))
                    {
                        // If we have an extension, get a node to use as reference for realm and such
                        // Extensions may be unstable in this state, though. So don't give them access to us and ignore their cries of pain
                        try
                        {
                            EntityNode refNode = GetNodeFromEntity(source.Internal, new EntityNodeWrangler());
                            refNode.OnCreation();
                            EventOutput refOutput = refNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event);

                            if (refOutput == null)
                            {
                                output.Realm = output.ParseRealm(eventConnection.TargetType.ToString());
                                continue;
                            }

                            if (refOutput.HasPlayer)
                            {
                                output.HasPlayer = refOutput.HasPlayer;
                            }
                            output.Realm = refOutput.Realm;
                            continue;
                        }
                        catch (Exception)
                        {
                            // Empty
                        }
                    }

                    output.Realm = output.ParseRealm(eventConnection.TargetType.ToString());
                }
            }
        }

        public BaseLogicRefEntity()
        {
            Inputs.Add(new PropertyInput("BlueprintTransform", this));
        }
    }
}