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
            catch (System.Exception ex)
            {
                // Callers only see false; keep the cause for device logs (IL2CPP/AOT issues).
                UnityEngine.Debug.LogWarning($"[NetworkSerialization] {typeof(T).Name} decode failed: {ex}");
                message = default!;
                return false;
            }
        }
    }
}
