using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Ptooey/Recipe")]
    public class RecipeSO : ScriptableObject
    {
        public string RecipeName;
        public List<IngredientType> RequiredIngredients;
        public int PointValue;
    }
}
