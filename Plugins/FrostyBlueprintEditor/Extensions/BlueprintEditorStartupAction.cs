using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Options;
using Frosty.Core;
using FrostySdk.Interfaces;

namespace BlueprintEditorPlugin.Extensions
{
    public class BlueprintEditorStartupAction : StartupAction
    {
        public override Action<ILogger> Action => (logger) =>
        {
            logger.Log("Updating Blueprint Editor options...");
            EditorOptions.Update();
            logger.Log("Registering Blueprint Editor extensions...");
            ExtensionsManager.Initiate();
            logger.Log("Getting Node Mapping Configs...");
            EntityMappingNode.Register();
        };
    }
}