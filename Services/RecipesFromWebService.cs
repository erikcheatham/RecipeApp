using System.Net.Http.Json;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class RecipesFromWebService : IRecipeService
{
    private readonly List<Recipe> _recipes = new();

    public RecipesFromWebService(HttpClient http)
    {
        try
        {
            var task = http.GetFromJsonAsync<RecipeListResponse>("api/recipes");
            var response = task.GetAwaiter().GetResult();

            if (response?.Recipes != null)
            {
                _recipes.AddRange(response.Recipes);
            }
        }
        catch
        {
            // Fallback to empty list if fetch fails
        }
    }

    public List<Recipe> GetAllRecipes() => _recipes;

    public Recipe? GetRecipeByTitle(string title) =>
        _recipes.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
}