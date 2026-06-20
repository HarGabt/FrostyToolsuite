using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;
using FrostyEditor;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    public class EntityMappingNode : EntityNode
    {
        public static Dictionary<string, string> EntityMappings { get; set; } = new Dictionary<string, string>();
        private StreamReader _reader;

        public void Load(string type)
        {
            Header = Object.GetType().Name; // TODO workaround: ObjectType isn't being assigned. We instead assign it ourselves
            
            _reader = new StreamReader(EntityMappings[type]);
            string line = ReadCleanLine();
            while (line != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    line = ReadCleanLine();
                    continue;
                }

                try
                {
                    ReadProperty(line);
                }
                catch (Exception)
                {
                    App.Logger.LogError("Unable to load property {0} in node mapping {1}", line, type);
                }
                
                line = ReadCleanLine();
            }
            
            _reader.Dispose();
        }

        public static void Register()
        {
            ExtractNodeMappings();

            foreach (string file in Directory.EnumerateFiles(@"BlueprintEditor\NodeMappings", "*.nmc"))
            {
                try
                {
                    StreamReader reader = new(file);
                    string line = reader.ReadLine();
                    if (!line!.StartsWith("Type"))
                    {
                        App.Logger.LogError("{0} did not start with the object's type. Please always include the object's type as the first property entry");
                        continue;
                    }
                
                    line = line.Replace(" = ", "=");
            
                    // We split by =" because a property should always look like `property="value"`
                    // Value can be anything, though. Such as "="
                    // If we just split by the char = as a result the value would be split too
                    string[] propertyArg = line.Split(new[] { "=\"" }, StringSplitOptions.None);
                
                    if (EntityMappings.ContainsKey(propertyArg[1].Trim('"')))
                        continue;
                    
                    EntityMappings.Add(propertyArg[1].Trim('"'), file);
                    reader.Dispose();
                }
                catch (Exception)
                {
                    App.Logger.Log("nmc {0} could not be loaded", file);
                }
            }
        }

        private void ReadProperty(string property)
        {
            // Stupid user input making me have to check everything
            property = property.Replace(" = ", "=");
            
            // We split by =" because a property should always look like `property="value"`
            // Value can be anything, though. Such as "="
            // If we just split by the char = as a result the value would be split too
            string[] propertyArg = property.Split(new[] { "=\"" }, StringSplitOptions.None);
            string value = propertyArg[1];
            value = value.Trim('"');
            Realm realm = Realm;
            
            if (value.Split(',').Length == 2)
            {
                if (!Enum.TryParse<Realm>(value.Split(',')[1].Trim(), out realm))
                {
                    realm = Realm;
                }
            }

            switch (propertyArg[0])
            {
                case "Type":
                    break;
                
                case "Header":
                {
                    Header = value;
                } break;
                
                case "InputEvent":
                {
                    AddInput(value, ConnectionType.Event, realm);
                } break;
                case "InputProperty":
                {
                    AddInput(value, ConnectionType.Property, realm);
                } break;
                case "InputLink":
                {
                    AddInput(value, ConnectionType.Link, realm);
                } break;
                
                case "OutputEvent":
                {
                    AddOutput(value, ConnectionType.Event, realm);
                } break;
                case "OutputProperty":
                {
                    AddOutput(value, ConnectionType.Property, realm);
                } break;
                case "OutputLink":
                {
                    AddOutput(value, ConnectionType.Link, realm);
                } break;
            }
        }

        private string ReadCleanLine()
        {
            string line = _reader.ReadLine();
            line = line?.Trim();

            if (!string.IsNullOrEmpty(line))
            {
                int commentPosition = line.IndexOf("//"); //Remove comments
                if (commentPosition != -1)
                {
                    line = line.Remove(commentPosition).Trim();
                }
            }

            return line;
        }

        private static void ExtractNodeMappings()
        {
            string destinationDir = @"BlueprintEditor\NodeMappings\";
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string embeddedResource in assembly.GetManifestResourceNames().Where(r => r.Contains("BlueprintEditor.Nodes.TypeMapping.Configs") && r.EndsWith(".nmc")))
            {
                string fileName = Path.Combine(destinationDir, embeddedResource.Split('.').ElementAtOrDefault(embeddedResource.Split('.').Length - 2) + ".nmc");
                if (File.Exists(fileName)) // Dont overwrite any existing nmc's
                    continue;

                using Stream stream = assembly.GetManifestResourceStream(embeddedResource);
                if (stream is not null)
                    File.WriteAllText(fileName, new StreamReader(stream).ReadToEnd());
            }
        }
    }
}