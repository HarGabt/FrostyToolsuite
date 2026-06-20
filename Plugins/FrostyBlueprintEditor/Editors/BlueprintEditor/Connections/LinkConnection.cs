using System.ComponentModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class LinkConnection : EntityConnection
    {
        public override ConnectionType Type => ConnectionType.Link;
        public override Realm Realm => Realm.ClientAndServer; // TODO: Link connection realms

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
            ((dynamic)Object).SourceField = source.Name;
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
            ((dynamic)Object).TargetField = target.Name;
        }
        
        public LinkConnection(LinkOutput source, LinkInput target, object obj) : base(source, target, obj)
        {
            Object = obj;

            Realm = target.Realm;
            UpdateStatus();
        }
        
        public LinkConnection(LinkOutput source, LinkInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("LinkConnection");
            
            UpdateSourceRef();
            UpdateTargetRef();

            FixRealm();
            UpdateStatus();
        }
        
        protected override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                    ((dynamic)Object).SourceField = Source.Name;
                    ((dynamic)Object).TargetField = Target.Name;
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