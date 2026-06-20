using System;
using System.IO;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes;
using Frosty.Core;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    public class GenerateBoilerPlateExtension : BlueprintMenuItemExtension
    {
        public override string SubLevelMenuName => "Developer";
        public override string DisplayName => "Generate Boilerplate";
        private static string BoilerPlatePath => $@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\BoilerPlate\";

        public override RelayCommand ButtonClicked => new(o =>
        {
            foreach (IVertex selectedVertex in GraphEditor.NodeWrangler.SelectedVertices)
            {
                if (selectedVertex is EntityNode node && !ExtensionsManager.EntityNodeExtensions.ContainsKey(node.ObjectType))
                {
                    BoilerPlateGenerator boilerPlate = new($"{BoilerPlatePath}{node.ObjectType}.cs");
                    
                    boilerPlate.WriteCode("using System.Collections.ObjectModel");
                    boilerPlate.WriteCode("using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports");
                    boilerPlate.WriteCode("using BlueprintEditorPlugin.Models.Nodes.Ports");
                    boilerPlate.WriteCode("using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections");
                    boilerPlate.WriteCode("using Frosty.Core.Controls");
                    boilerPlate.WriteBlank();
                    
                    boilerPlate.WriteIndentedLine("namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared");
                    boilerPlate.WriteIndentedLine("{");
                    boilerPlate.NextLevel();
                    
                    boilerPlate.WriteClassHeader($"{node.ObjectType.Replace("EntityData", "")}Node", "EntityNode");
                    boilerPlate.WriteIndentedLine("{");
                    boilerPlate.NextLevel();
                    
                    boilerPlate.WriteCode($"public override string ObjectType => \"{node.ObjectType}\"");
                    boilerPlate.WriteBlank();
                    
                    boilerPlate.WriteMethodHeader("OnCreation", "void", MethodType.Override);
                    boilerPlate.WriteIndentedLine("{");
                    boilerPlate.NextLevel();
                    boilerPlate.WriteCode("base.OnCreation()");
                    boilerPlate.WriteBlank();

                    foreach (EntityInput input in node.Inputs)
                    {
                        boilerPlate.WriteMethodCode("AddInput", $"\"{input.Name}\"", $"ConnectionType.{input.Type}", "Realm");
                    }
                    
                    boilerPlate.WriteBlank();
                    
                    foreach (EntityOutput output in node.Outputs)
                    {
                        boilerPlate.WriteMethodCode("AddOutput", $"\"{output.Name}\"", $"ConnectionType.{output.Type}", "Realm");
                    }
                    
                    boilerPlate.PreviousLevel();
                    boilerPlate.WriteIndentedLine("}");
                    boilerPlate.PreviousLevel();
                    boilerPlate.WriteIndentedLine("}");
                    boilerPlate.PreviousLevel();
                    boilerPlate.WriteIndentedLine("}");
                    
                    boilerPlate.Dispose();
                }
            }
        });
    }

    internal class BoilerPlateGenerator : IDisposable
    {
        private StreamWriter _writer;
        public int Level;

        #region Indentation management

        public void WriteIndentedLine(string line)
        {
            _writer.WriteLine($"{GetIndentation()}{line}");
        }
        
        public void PreviousLevel()
        {
            if (Level == 0)
                return;
            Level--;
        }

        public void NextLevel()
        {
            Level++;
        }

        /// <summary>
        /// Get the amount of indentations we need based on the current level.
        /// </summary>
        /// <returns>Indentations for class properties on this level</returns>
        private string GetIndentation()
        {
            string indentations = "";
            for (int i = 0; i < Level; i++)
            {
                indentations += "\t";
            }

            return indentations;
        }

        #endregion

        public void WriteClassHeader(string name, string extension = null)
        {
            if (extension == null)
            {
                WriteIndentedLine($"public class {name}");
            }
            else
            {
                WriteIndentedLine($"public class {name} : {extension}");
            }
        }

        public void WriteMethodHeader(string name, string returnType, MethodType type, params string[] paramters)
        {
            string header = "";
            switch (type)
            {
                case MethodType.Default:
                {
                    header += $"public {returnType} {name}";
                } break;
                case MethodType.Override:
                {
                    header += $"public override {returnType} {name}";
                } break;
                case MethodType.Sealed:
                {
                    header += $"public sealed {returnType} {name}";
                } break;
                case MethodType.Abstract:
                {
                    // Abstract cannot have a body, so we just write its params and call it there
                    header += $"public abstract {returnType} {name}(";
                    for (var index = 0; index < paramters.Length; index++)
                    {
                        string paramter = paramters[index];
                        if (index != 0)
                        {
                            header += ", ";
                        }

                        header += paramter;
                    }

                    header += ");";
                    WriteIndentedLine(header);
                    return;
                }
            }

            header += "(";
            for (var index = 0; index < paramters.Length; index++)
            {
                string paramter = paramters[index];
                if (index != 0)
                {
                    header += ", ";
                }

                header += paramter;
            }

            header += ")";
            WriteIndentedLine(header);
        }

        public void WriteMethodCode(string name, params string[] paramters)
        {
            string method = name;
            method += "(";
            for (var index = 0; index < paramters.Length; index++)
            {
                string paramter = paramters[index];
                if (index != 0)
                {
                    method += ", ";
                }

                method += paramter;
            }

            method += ")";
            WriteCode(method);
        }

        public void WriteCode(string line)
        {
            WriteIndentedLine($"{line};");
        }

        public void WriteBlank(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                _writer.WriteLine("");
            }
        }

        public BoilerPlateGenerator(string path)
        {
            FileInfo fileInfo = new($@"{path}");
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
            
            _writer = new(path);
        }
        
        public void Dispose()
        {
            _writer.Dispose();
        }
    }
    
    internal enum MethodType
    {
        Default = 0,
        Override = 1,
        Sealed = 2,
        Abstract = 3
    }
}