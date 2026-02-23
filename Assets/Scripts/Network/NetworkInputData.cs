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
        public NetworkBool StealInput;

        public float MouseX; // horizontal mouse delta
        public float MouseY; // vertical mouse delta
    }
}
