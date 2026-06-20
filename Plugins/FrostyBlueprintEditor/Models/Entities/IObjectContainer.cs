using System;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Entities
{
    public interface IObjectContainer
    {
        object Object { get; }

        void OnObjectModified(object sender, ItemModifiedEventArgs args);
    }

    public interface IEntityObject : IObjectContainer
    {
        PointerRefType Type { get; }
        AssetClassGuid InternalGuid { get; }
        Guid FileGuid { get; }
        Guid ClassGuid { get; }
    }
}