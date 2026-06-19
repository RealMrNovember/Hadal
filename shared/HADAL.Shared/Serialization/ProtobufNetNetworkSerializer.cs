using System.IO;
using ProtoBuf;

namespace HADAL.Shared.Serialization
{
    /// <summary>
    /// protobuf-net implementation of <see cref="INetworkSerializer"/> — shared by client and backend.
    /// </summary>
    public sealed class ProtobufNetNetworkSerializer : INetworkSerializer
    {
        public uint SchemaVersion => HADAL.Shared.Serialization.SchemaVersion.Current;

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

        public bool TryDeserialize<T>(byte[] payload, out T? message)
        {
            try
            {
                message = Deserialize<T>(payload);
                return true;
            }
            catch
            {
                message = default;
                return false;
            }
        }
    }
}
