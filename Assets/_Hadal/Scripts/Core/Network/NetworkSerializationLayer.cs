using System.IO;
using ProtoBuf;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Unity client serialization facade — protobuf-net contract serialization.
    /// </summary>
    public sealed class NetworkSerializationLayer
    {
        public byte[] Serialize<T>(T message)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        public T Deserialize<T>(byte[] payload)
        {
            using var stream = new MemoryStream(payload);
            return Serializer.Deserialize<T>(stream);
        }

        public bool TryDeserialize<T>(byte[] payload, out T message)
        {
            try
            {
                message = Deserialize<T>(payload);
                return true;
            }
            catch
            {
                message = default!;
                return false;
            }
        }
    }
}
