using System;
using Unity.Netcode;

namespace Utils
{
    
    // adapted from Boss Room: Small Scale Co-op Sample © 2021 Unity Technologies
    // https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/e48babbbf2e903d65e08a114aac3f3eba824a94a/Assets/Scripts/Infrastructure/NetworkGuid.cs
    
    public struct NetworkGuid : INetworkSerializeByMemcpy
    {
        public ulong FirstHalf;
        public ulong SecondHalf;
    }

    public static class NetworkGuidExtensions
    {
        public static NetworkGuid ToNetworkGuid(this Guid id)
        {
            return new NetworkGuid
            {
                FirstHalf = BitConverter.ToUInt64(id.ToByteArray(), 0),
                SecondHalf = BitConverter.ToUInt64(id.ToByteArray(), 8)
            };
        }

        public static Guid ToGuid(this NetworkGuid networkId)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.FirstHalf), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.SecondHalf), 0, bytes, 8, 8);
            return new Guid(bytes);
        }
    }
}