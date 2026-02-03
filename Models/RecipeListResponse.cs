namespace RecipeApp.Models;

public class RecipeListResponse
{
    public List<Recipe> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
}