using System.Net.Http.Json;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class RecipesFromWebService : IRecipeService
{
    private readonly HttpClient _http;

    public RecipesFromWebService(HttpClient http)
    {
        _http = http;
    }

    public List<Recipe> GetAllRecipes()
    {
        try
        {
            var task = _http.GetFromJsonAsync<RecipeListResponse>("api/recipes");
            var response = task.GetAwaiter().GetResult();
            return response?.Recipes ?? new List<Recipe>();
        }
        catch
        {
            return new List<Recipe>();
        }
    }

    public Recipe? GetRecipeByTitle(string title)
    {
        var all = GetAllRecipes();
        return all.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    public void AddRecipe(Recipe recipe)
    {
        var response = _http.PostAsJsonAsync("api/recipes", recipe).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException($"Failed to add: {response.StatusCode} – {error}");
        }
    }

    public void UpdateRecipe(string originalTitle, Recipe updatedRecipe)
    {
        var encodedTitle = Uri.EscapeDataString(originalTitle);
        var response = _http.PutAsJsonAsync($"api/recipes/{encodedTitle}", updatedRecipe).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException($"Failed to update: {response.StatusCode} – {error}");
        }
    }

    public void DeleteRecipe(string title)
    {
        var encodedTitle = Uri.EscapeDataString(title);
        var response = _http.DeleteAsync($"api/recipes/{encodedTitle}").GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException($"Failed to delete: {response.StatusCode} – {error}");
        }
    }
}