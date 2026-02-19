using Fusion;
using UnityEngine;

namespace Network
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 InputVector;
        public NetworkBool JumpInput;
        public NetworkBool SprintInput;
        public NetworkBool InteractInput;
    }
}
