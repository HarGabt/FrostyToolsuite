using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Entities.Networking;
using BlueprintEditorPlugin.Models.Status;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class PropertyConnection : EntityConnection
    {
        public override ConnectionType Type => ConnectionType.Property;

        private Realm _realm;
        public override Realm Realm
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
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType, SourceCantBeStatic);
                NotifyPropertyChanged(nameof(Realm));
                UpdateStatus();
            }
        }

        private bool _isDynamic;
        public bool SourceCantBeStatic
        {
            get => _isDynamic;
            set
            {
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType, SourceCantBeStatic);
                _isDynamic = value;
                NotifyPropertyChanged(nameof(SourceCantBeStatic));
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

        public override void UpdateStatus()
        {
            base.UpdateStatus();

            if (PropType == PropertyType.Invalid)
            {
                SetStatus(EditorStatus.Broken, "Property type is invalid");
            }

            EntityPort target = (EntityPort)Target;
            EntityPort source = (EntityPort)Source;
            if (PropType == PropertyType.Interface && !(target.IsInterface || source.IsInterface))
            {
                SetStatus(EditorStatus.Flawed, "Property type is set to interface, despite not plugging into an interface");
            }
        }

        public PropertyConnection(PropertyOutput source, PropertyInput target, object obj) : base(source, target, obj)
        {
            Object = obj;

            PropertyFlagsHelper.GetAsRealm(((dynamic)Object).Flags, out Realm realm, out  PropertyType propType, out bool isDynamic);

            Realm = realm;
            PropType = propType;
            SourceCantBeStatic = isDynamic;
            
            UpdateStatus();
        }
        
        public PropertyConnection(PropertyOutput source, PropertyInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("PropertyConnection");
            PropType = target.IsInterface ? PropertyType.Interface : PropertyType.Default;
            
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
                case "IsInterface":
                {
                    if (((EntityPort)Target).IsInterface)
                    {
                        PropType = PropertyType.Interface;
                    }
                    else
                    {
                        PropType = PropertyType.Default;
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

    public static class PropertyFlagsHelper
    {
        public static uint GetAsFlags(Realm realm, PropertyType propType, bool sourceCantBeStatic = false)
        {
            uint flags = 0;
            flags |= (uint)realm;
            flags |= ((uint)propType) << 4;
            if (sourceCantBeStatic)
            {
                flags |= 8;
            }

            return flags;
        }

        public static void GetAsRealm(uint flags, out Realm realm, out PropertyType propType, out bool sourceCantBeStatic)
        {
            realm = (Realm)(flags & 7);
            propType = (PropertyType)((flags & 48) >> 4);
            sourceCantBeStatic = Convert.ToBoolean((flags & 8) != 0 ? 1 : 0);
        }

        public static Realm GetAsRealm(uint flags)
        {
            return (Realm)(flags & 7);
        }
        
        public static PropertyType GetAsPropType(uint flags)
        {
            return (PropertyType)(flags & 7);
        }
    }
}