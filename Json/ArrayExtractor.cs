using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vrm.Json
{
    public class ArrayExtractor
    {
        public static Dictionary<string, List<string>> ExtractJsonArraysAll(string filePath, params string[] arrayNames)
        {
            var foundArrays = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var namesToFind = new HashSet<string>(arrayNames, StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(filePath))
            using (var jsonReader = new JsonTextReader(reader))
            {
                jsonReader.SupportMultipleContent = true;

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName && namesToFind.Contains((string)jsonReader.Value))
                    {
                        string currentName = (string)jsonReader.Value;

                        if (jsonReader.Read() && jsonReader.TokenType == JsonToken.StartArray)
                        {
                            var sb = new StringBuilder();
                            var sw = new StringWriter(sb);
                            using (var writer = new JsonTextWriter(sw))
                            {
                                writer.Formatting = Formatting.Indented;
                                writer.WriteStartArray();

                                while (jsonReader.Read())
                                {
                                    if (jsonReader.TokenType == JsonToken.EndArray)
                                        break;

                                    var item = JToken.ReadFrom(jsonReader);
                                    item.WriteTo(writer);
                                }

                                writer.WriteEndArray();
                            }

                            if (!foundArrays.TryGetValue(currentName, out var list))
                            {
                                list = new List<string>();
                                foundArrays[currentName] = list;
                            }

                            list.Add(sb.ToString());
                        }
                    }
                }
            }

            return foundArrays;
        }
    }
}
