using Unity.Netcode;

namespace Networking.RpcRequestStructs
{
    public struct MissionData : INetworkSerializable
    {
        public string missionName;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref missionName);
        }
    }
}