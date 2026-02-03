using RecipeApp.Models;

namespace RecipeApp.Services;

public interface IRecipeService
{
    List<Recipe> GetAllRecipes();
    Recipe? GetRecipeByTitle(string title);
    void AddRecipe(Recipe recipe);
    void UpdateRecipe(string originalTitle, Recipe updatedRecipe);
    void DeleteRecipe(string title);
}