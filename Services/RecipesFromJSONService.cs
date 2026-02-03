using RecipeApp.Models;
using System.Text.Json;

namespace RecipeApp.Services;

public class RecipesFromJSONService : IRecipeService
{
    private readonly List<Recipe> _recipes = new();

    public RecipesFromJSONService(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "recipes.json");

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var loaded = JsonSerializer.Deserialize<List<Recipe>>(json, options);

            if (loaded != null)
            {
                _recipes.AddRange(loaded);
            }
        }
        // If file missing or empty, _recipes stays empty (you could add fallback hard-coding here if desired)
    }

    public List<Recipe> GetAllRecipes() => _recipes;

    public Recipe? GetRecipeByTitle(string title) =>
        _recipes.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
}