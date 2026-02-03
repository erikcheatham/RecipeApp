using System.ComponentModel.DataAnnotations;

namespace RecipeApp.Models;

public class Recipe
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Yield must be between 1 and 100")]
    public int Yield { get; set; }

    public List<string> Ingredients { get; set; } = new();
}