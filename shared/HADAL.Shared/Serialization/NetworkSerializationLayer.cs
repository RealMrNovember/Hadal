namespace HADAL.Shared.Serialization
{
    /// <summary>
    /// Facade over <see cref="INetworkSerializer"/> with payload validation hooks.
    /// Protobuf code generation will replace manual serialization in a later sprint.
    /// </summary>
    public sealed class NetworkSerializationLayer
    {
        private readonly INetworkSerializer _serializer;

        public NetworkSerializationLayer(INetworkSerializer serializer)
        {
            _serializer = serializer;
        }

        public uint SchemaVersion => _serializer.SchemaVersion;

        public byte[] Serialize<T>(T message) => _serializer.Serialize(message);

        public T Deserialize<T>(byte[] payload) => _serializer.Deserialize<T>(payload);
    }
}
