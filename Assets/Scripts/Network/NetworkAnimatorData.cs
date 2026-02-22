using Fusion;

namespace Network
{
    public struct NetworkAnimatorData : INetworkStruct
    {
        public float Speed;
        public NetworkBool IsCrouching;
        public NetworkBool IsHoldingItem;

        // Use a counter for actions like Jump or Throw
        // This ensures the animation plays even if the network is laggy
        public byte JumpCount;
        public byte ThrowCount;
        public byte PickupCount;
    }
}