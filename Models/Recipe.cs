namespace RecipeApp.Models;

public class Recipe
{
    public string Title { get; set; } = string.Empty;
    public int Yield { get; set; }
    public List<string> Ingredients { get; set; } = new();
}