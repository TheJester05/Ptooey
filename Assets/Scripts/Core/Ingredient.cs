using Fusion;
using UnityEngine;

namespace Core
{
    public class Ingredient : NetworkBehaviour
    {
        [Networked] public IngredientType Type { get; set; }
        [Networked] public NetworkBool IsHeld { get; set; }
        [Networked] public PlayerRef HeldBy { get; set; }

        public override void Spawned()
        {
            // Visual feedback could be added here
        }

        public void SetHeld(PlayerRef player, bool held)
        {
            IsHeld = held;
            HeldBy = player;
        }
    }
}
