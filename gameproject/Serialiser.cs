using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gameproject
{
    public static class Serialiser
    {
            public static byte[] Serialize<T>(T obj)
            {
                var serializer = new DataContractSerializer(typeof(T));
                var stream = new MemoryStream();
                using (var writer =
                    XmlDictionaryWriter.CreateBinaryWriter(stream))
                {
                    serializer.WriteObject(writer, obj);
                }
                return stream.ToArray();
            }

            public static T Deserialize<T>(byte[] data)
            {
                var serializer = new DataContractSerializer(typeof(T));
                using (var stream = new MemoryStream(data))
                using (var reader =
                    XmlDictionaryReader.CreateBinaryReader(
                        stream, XmlDictionaryReaderQuotas.Max))
                {
                    return (T)serializer.ReadObject(reader);
                }
            }
    }
}
