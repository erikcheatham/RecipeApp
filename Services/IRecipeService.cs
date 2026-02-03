using RecipeApp.Models;

namespace RecipeApp.Services;

public interface IRecipeService
{
    List<Recipe> GetAllRecipes();
    Recipe? GetRecipeByTitle(string title);
}