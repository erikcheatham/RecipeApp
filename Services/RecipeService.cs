using RecipeApp.Models;

namespace RecipeApp.Services;

public class RecipeService : IRecipeService
{
    private readonly List<Recipe> _recipes = new();

    public RecipeService()
    {
        // existing hard-coded data...
        _recipes.Add(new Recipe
        {
            Title = "Chicken Stir-Fry",
            Yield = 2,
            Ingredients = new List<string>
            {
                "30g olive oil",
                "200g chicken breast",
                "500g broccoli florets",
                "250g red bell pepper",
                "60g soy sauce",
                "10g garlic",
                "5g ginger"
            }
        });

        _recipes.Add(new Recipe
        {
            Title = "Veggie Omelette",
            Yield = 1,
            Ingredients = new List<string>
            {
                "100g large eggs",
                "40g diced onion",
                "50g diced tomato",
                "20g spinach leaves",
                "15g olive oil",
                "150g small corn tortillas",
                "3g Salt and pepper (to taste)"
            }
        });

        _recipes.Add(new Recipe
        {
            Title = "Overnight Oats",
            Yield = 4,
            Ingredients = new List<string>
            {
                "160g rolled oats",
                "1000g almond milk",
                "70g chia seeds",
                "60g honey",
                "300g blueberries"
            }
        });
    }

    public List<Recipe> GetAllRecipes() => _recipes;

    public Recipe? GetRecipeByTitle(string title) =>
        _recipes.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    public void AddRecipe(Recipe recipe)
    {
        if (_recipes.Any(r => r.Title.Equals(recipe.Title, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A recipe with this title already exists.");

        _recipes.Add(recipe);
    }

    public void UpdateRecipe(string originalTitle, Recipe updatedRecipe)
    {
        var existing = GetRecipeByTitle(originalTitle);
        if (existing == null) throw new InvalidOperationException("Recipe not found.");

        var newTitle = updatedRecipe.Title;
        if (!newTitle.Equals(originalTitle, StringComparison.OrdinalIgnoreCase) &&
            _recipes.Any(r => r.Title.Equals(newTitle, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A recipe with the new title already exists.");

        existing.Title = updatedRecipe.Title;
        existing.Yield = updatedRecipe.Yield;
        existing.Ingredients.Clear();
        existing.Ingredients.AddRange(updatedRecipe.Ingredients);
    }

    public void DeleteRecipe(string title) =>
        _recipes.RemoveAll(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
}