namespace HADAL.Shared.Serialization
{
    /// <summary>
    /// Protobuf encode/decode entry point — shared by Unity client and backend.
    /// </summary>
    public interface INetworkSerializer
    {
        uint SchemaVersion { get; }

        byte[] Serialize<T>(T message);

        T Deserialize<T>(byte[] payload);

        bool TryDeserialize<T>(byte[] payload, out T? message);
    }
}
