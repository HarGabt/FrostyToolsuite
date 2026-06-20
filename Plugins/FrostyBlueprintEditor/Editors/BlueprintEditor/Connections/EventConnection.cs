using System;
using System.ComponentModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class EventConnection : EntityConnection
    {
        public override ConnectionType Type => ConnectionType.Event;

        private Realm _realm;
        public sealed override Realm Realm
        {
            get => _realm;
            set
            {
                if (value == Realm.Any)
                {
                    App.Logger.LogError("Cannot set the realm of a connection to any.");
                    return;
                }
                
                _realm = value;
                Type realmType = ((dynamic)Object).TargetType.GetType();
                ((dynamic)Object).TargetType = (dynamic)Enum.Parse(realmType, $"EventConnectionTargetType_{_realm.ToString()}");
                NotifyPropertyChanged(nameof(Realm));
                UpdateStatus();
            }
        }

        public sealed override void UpdateSourceRef(EntityPort source)
        {
            base.UpdateSourceRef(source);
            
            PointerRef sourceRef;
            if (((IEntityNode)source.Node).Type == PointerRefType.Internal)
            {
                sourceRef = new PointerRef(((IEntityNode)source.Node).Object);
            }
            else
            {
                sourceRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IEntityNode)source.Node).FileGuid,
                    ClassGuid = ((IEntityNode)source.Node).ClassGuid
                });
            }
            
            ((dynamic)Object).Source = sourceRef;
            ((dynamic)Object).SourceEvent.Name = source.Name;
        }

        public sealed override void UpdateTargetRef(EntityPort target)
        {
            base.UpdateTargetRef(target);
            
            PointerRef targetRef;
            if (((IEntityNode)target.Node).Type == PointerRefType.Internal)
            {
                targetRef = new PointerRef(((IEntityNode)target.Node).Object);
            }
            else
            {
                targetRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IEntityNode)target.Node).FileGuid,
                    ClassGuid = ((IEntityNode)target.Node).ClassGuid
                });
            }
            
            ((dynamic)Object).Target = targetRef;
            ((dynamic)Object).TargetEvent.Name = target.Name;
        }

        public EventConnection(EventOutput source, EventInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("EventConnection");
            
            UpdateSourceRef();
            UpdateTargetRef();

            HasPlayer = source.HasPlayer;

            // lazy way to double check the node if we lack a player event, just to be sure
            if (!HasPlayer && source.Node is EntityNode entityNode)
            {
                HasPlayer = entityNode.HasPlayerEvent;
            }
            
            if (HasPlayer && target.Node is EntityNode targetNode)
            {
                targetNode.HasPlayerEvent = HasPlayer;
            }

            FixRealm();
            UpdateStatus();
        }
        
        public EventConnection(EventOutput source, EventInput target, object obj) : base(source, target, obj)
        {
            Object = obj;

            HasPlayer = source.HasPlayer;
            
            // lazy way to double check the node if we lack a player event, just to be sure
            if (!HasPlayer && source.Node is EntityNode entityNode)
            {
                HasPlayer = entityNode.HasPlayerEvent;
            }

            if (HasPlayer && target.Node is EntityNode targetNode)
            {
                targetNode.HasPlayerEvent = HasPlayer;
            }

            Realm = ParseRealm(((dynamic)Object).TargetType.ToString());
            UpdateStatus();
        }

        protected sealed override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsSelected":
                {
                    IsSelected = Source.Node.IsSelected || Target.Node.IsSelected;
                    NotifyPropertyChanged(nameof(IsSelected));
                } break;
                case "Realm":
                {
                    UpdateStatus();
                } break;
                case "Name":
                {
                    ((dynamic)Object).SourceEvent.Name = Source.Name;
                    ((dynamic)Object).TargetEvent.Name = Target.Name;
                } break;
                case "HasPlayerEvent":
                case "HasPlayer":
                {
                    if (sender == Target || sender == Target.Node)
                        break;
                    
                    // If this is identical, no point in bothering to change it
                    if (HasPlayer == ((EntityPort)Source).HasPlayer && Target.Node is EntityNode targetNode && HasPlayer == targetNode.HasPlayerEvent)
                        return;
                    
                    HasPlayer = ((EntityPort)Source).HasPlayer || ((EntityNode)Source.Node).HasPlayerEvent;
                    if (Target.Node is EntityNode entityNode)
                    {
                        if (HasPlayer == entityNode.HasPlayerEvent)
                            return;
                        
                        entityNode.HasPlayerEvent = HasPlayer;
                    }
                } break;
                case "Node":
                {
                    PointerRef sourceRef;

                    EntityPort source = (EntityPort)Source;
                    EntityPort target = (EntityPort)Target;
                    
                    if (((IEntityNode)source.Node).Type == PointerRefType.Internal)
                    {
                        sourceRef = new PointerRef(((IEntityNode)source.Node).Object);
                    }
                    else
                    {
                        sourceRef = new PointerRef(new EbxImportReference()
                        {
                            FileGuid = ((IEntityNode)source.Node).FileGuid,
                            ClassGuid = ((IEntityNode)source.Node).ClassGuid
                        });
                    }
            
                    PointerRef targetRef;
                    if (((IEntityNode)target.Node).Type == PointerRefType.Internal)
                    {
                        targetRef = new PointerRef(((IEntityNode)target.Node).Object);
                    }
                    else
                    {
                        targetRef = new PointerRef(new EbxImportReference()
                        {
                            FileGuid = ((IEntityNode)target.Node).FileGuid,
                            ClassGuid = ((IEntityNode)target.Node).ClassGuid
                        });
                    }

                    ((dynamic)Object).Source = sourceRef;
                    ((dynamic)Object).Target = targetRef;
                } break;
            }
        }
    }
}