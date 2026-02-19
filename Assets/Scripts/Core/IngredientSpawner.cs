using Fusion;
using UnityEngine;

namespace Core
{
    public class IngredientSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef ingredientPrefab;
        [SerializeField] private IngredientType ingredientType;
        [SerializeField] private float spawnCooldown = 3f;

        [Networked] private TickTimer spawnTimer { get; set; }
        [Networked] private NetworkObject currentIngredient { get; set; }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (currentIngredient == null && spawnTimer.ExpiredOrNotRunning(Runner))
            {
                SpawnIngredient();
            }
            else if (currentIngredient != null)
            {
                // Check if the ingredient was picked up
                var ingredient = currentIngredient.GetComponent<Ingredient>();
                if (ingredient != null && ingredient.IsHeld)
                {
                    currentIngredient = null;
                    spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnCooldown);
                }
            }
        }

        private void SpawnIngredient()
        {
            var spawnedObject = Runner.Spawn(ingredientPrefab, transform.position, Quaternion.identity, PlayerRef.None);
            currentIngredient = spawnedObject;
            var ingredient = spawnedObject.GetComponent<Ingredient>();
            if (ingredient != null)
            {
                ingredient.Type = ingredientType;
            }
        }
    }
}
