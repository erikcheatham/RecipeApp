using RecipeApp.Models;
using System.Text.Json;

namespace RecipeApp.Services;

public class RecipesFromJSONService : IRecipeService
{
    private readonly IWebHostEnvironment _environment;

    public RecipesFromJSONService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public List<Recipe> GetAllRecipes() => LoadRecipes();

    public Recipe? GetRecipeByTitle(string title)
    {
        var all = GetAllRecipes();
        return all.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    public void AddRecipe(Recipe recipe)
    {
        var list = GetAllRecipes();
        if (list.Any(r => r.Title.Equals(recipe.Title, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A recipe with this title already exists.");

        list.Add(recipe);
        SaveRecipes(list);
    }

    public void UpdateRecipe(string originalTitle, Recipe updatedRecipe)
    {
        var list = GetAllRecipes();
        var index = list.FindIndex(r => r.Title.Equals(originalTitle, StringComparison.OrdinalIgnoreCase));
        if (index == -1) throw new InvalidOperationException("Recipe not found.");

        var newTitle = updatedRecipe.Title;
        if (!newTitle.Equals(originalTitle, StringComparison.OrdinalIgnoreCase) &&
            list.Any(r => r.Title.Equals(newTitle, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A recipe with the new title already exists.");

        var existing = list[index];
        existing.Title = newTitle;
        existing.Yield = updatedRecipe.Yield;
        existing.Ingredients.Clear();
        existing.Ingredients.AddRange(updatedRecipe.Ingredients);

        SaveRecipes(list);
    }

    public void DeleteRecipe(string title)
    {
        var list = GetAllRecipes();
        var removed = list.RemoveAll(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        if (removed == 0) throw new InvalidOperationException("Recipe not found.");

        SaveRecipes(list);
    }

    private List<Recipe> LoadRecipes()
    {
        var path = Path.Combine(_environment.ContentRootPath, "Data", "recipes.json");
        if (!File.Exists(path)) return new List<Recipe>();

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<Recipe>>(json, options) ?? new List<Recipe>();
    }

    private void SaveRecipes(List<Recipe> recipes)
    {
        var path = Path.Combine(_environment.ContentRootPath, "Data", "recipes.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(recipes, options);
        File.WriteAllText(path, json);
    }
}