using System.Collections.Generic;
using Fusion;
using Network;
using UnityEngine;

namespace Core
{
    public class Pot : NetworkBehaviour
    {
        [Networked] public int CurrentRecipeIndex { get; set; } = -1; // networkable index

        [Networked, Capacity(10)]
        public NetworkLinkedList<IngredientType> AddedIngredients => default;

        [SerializeField] public List<RecipeSO> possibleRecipes;

        [SerializeField] private int pointsPerIngredient = 1;      // per ingredient points
        [SerializeField] private int recipeCompletionBonus = 50;   // bonus for completing recipe

        // Expose current RecipeSO as a property
        public RecipeSO CurrentRecipe
        {
            get
            {
                if (CurrentRecipeIndex >= 0 && CurrentRecipeIndex < possibleRecipes.Count)
                    return possibleRecipes[CurrentRecipeIndex];
                return null;
            }
        }

        // Called by player when adding an ingredient
        public bool TryAddIngredient(IngredientType type, PlayerRef player)
        {
            if (!HasStateAuthority) return false;

            AddedIngredients.Add(type);

            // Award points for each ingredient added
            NetworkedGameManager.Instance.AwardPoints(player, pointsPerIngredient);

            CheckRecipes(player);

            return true;
        }

        // Check if current recipe is complete
        private void CheckRecipes(PlayerRef player)
        {
            if (CurrentRecipe == null) return;

            if (IsRecipeComplete(CurrentRecipe))
            {
                Debug.Log($"Recipe Complete: {CurrentRecipe.RecipeName}");

                // Award points for completing the recipe
                NetworkedGameManager.Instance.AwardPoints(player, CurrentRecipe.PointValue + recipeCompletionBonus);

                // Clear ingredients and pick a new random recipe
                AddedIngredients.Clear();
                SetRandomRecipe();
            }
        }

        // Simple recipe completion check
        private bool IsRecipeComplete(RecipeSO recipe)
        {
            if (AddedIngredients.Count < recipe.RequiredIngredients.Count) return false;

            List<IngredientType> current = new List<IngredientType>();
            foreach (var ing in AddedIngredients) current.Add(ing);

            foreach (var req in recipe.RequiredIngredients)
            {
                if (!current.Contains(req)) return false;
                current.Remove(req); // handle duplicates
            }

            return true;
        }

        // Pick a random recipe
        private void SetRandomRecipe()
        {
            if (possibleRecipes == null || possibleRecipes.Count == 0) return;

            int index = Random.Range(0, possibleRecipes.Count);
            CurrentRecipeIndex = index;

            Debug.Log($"New Current Recipe: {CurrentRecipe.RecipeName}");
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                SetRandomRecipe();
            }
        }
    }
}