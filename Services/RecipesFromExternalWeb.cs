using System.Net.Http.Json;
using RecipeApp.Models;

namespace RecipeApp.Services;

//public class RecipesFromWebService : IRecipeService
//{
//    private readonly HttpClient _http;

//    public RecipesFromWebService(HttpClient http)
//    {
//        _http = http;
//    }

//    public List<Recipe> GetAllRecipes()
//    {
//        try
//        {
//            var task = _http.GetFromJsonAsync<RecipeListResponse>("https://faculty-web.msoe.edu/yoder/macro.csv");
//            var response = task.GetAwaiter().GetResult();
//            return response?.Recipes ?? new List<Recipe>();
//        }
//        catch
//        {
//            return new List<Recipe>();
//        }
//    }

//    public Recipe? GetRecipeByTitle(string title)
//    {
//        var all = GetAllRecipes();
//        return all.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
//    }
//}