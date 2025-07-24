using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Vrm.Json
{
    public static class JsonModifiedParser
    {
        public static T LoadIfModifiedEquals<T>(string filePath, string dateTimeProperty, DateTime target)
        {
            using (var sr = new StreamReader(filePath))
            {
                using (var reader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    DateTime? foundDate = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName && string.Equals((string)reader.Value, dateTimeProperty, StringComparison.OrdinalIgnoreCase))
                        {
                            reader.Read();
                            if (reader.TokenType == JsonToken.Date)
                            {
                                if (reader.Value != null)
                                    foundDate = (DateTime)reader.Value;
                            }
                            else if (reader.TokenType == JsonToken.String)
                            {
                                foundDate = DateTime.Parse((string)reader.Value);
                            }

                            break;
                        }
                    }

                    if (foundDate.HasValue && foundDate.Value == target)
                    {
                        sr.BaseStream.Seek(0, SeekOrigin.Begin);
                        sr.DiscardBufferedData();
                        return serializer.Deserialize<T>(new JsonTextReader(sr));
                    }

                    return default(T);
                }
            }
        }
    }
}
