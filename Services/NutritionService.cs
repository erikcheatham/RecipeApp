using RecipeApp.Models;
using CsvHelper;
using System.Globalization;
using FuzzySharp;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace RecipeApp.Services;

public class NutritionService
{
    private readonly List<FoodMatch> _foods = new();
    private readonly Regex _quantityRegex = new(@"^(\d+(\.\d+)?)\s*([a-zA-Z]+)?\s*(.+)$", RegexOptions.Compiled);

    public NutritionService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "macro.csv");
        if (!File.Exists(path)) return;

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>();

        foreach (var record in records)
        {
            var dict = (IDictionary<string, object>)record;
            _foods.Add(new FoodMatch
            {
                Description = dict["description"].ToString()!.Trim().ToLower(),
                Calories = double.TryParse(dict["calories"].ToString(), out var cal) ? cal : 0,
                Protein = double.TryParse(dict["proteinInGrams"].ToString(), out var pro) ? pro : 0,
                Carbs = double.TryParse(dict["carbohydratesInGrams"].ToString(), out var carb) ? carb : 0,
                Fat = double.TryParse(dict["fatInGrams"].ToString(), out var fat) ? fat : 0
            });
        }
    }

    public NutritionProfile ComputeNutrition(Recipe recipe)
    {
        var total = new NutritionProfile();
        recipe.IngredientMatches = new List<FoodMatch>();

        foreach (var ing in recipe.Ingredients)
        {
            var match = ParseAndMatchIngredient(ing);
            if (match != null)
            {
                recipe.IngredientMatches.Add(match);

                // Scale by quantity / 100g
                var scale = match.MatchScore / 100.0;  // Rough confidence adjustment
                total.Calories += match.Calories * scale;
                total.Protein += match.Protein * scale;
                total.Carbs += match.Carbs * scale;
                total.Fat += match.Fat * scale;
            }
        }

        recipe.TotalNutrition = total;
        recipe.PerServingNutrition = total.PerServing(recipe.Yield);

        return total;
    }

    private FoodMatch? ParseAndMatchIngredient(string ingredient)
    {
        // Parse quantity (e.g., "30g olive oil" -> quantity 30, name "olive oil")
        var m = _quantityRegex.Match(ingredient);
        if (!m.Success) return null;

        var quantityStr = m.Groups[1].Value;
        var name = m.Groups[4].Value.Trim().ToLower();

        if (!double.TryParse(quantityStr, out var quantity)) return null;

        // Find best match (TokenSetRatio best for partial/phrase matches)
        var best = _foods
            .Select(f => new { Food = f, Score = Fuzz.TokenSetRatio(name, f.Description) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault(x => x.Score > 70);  // Threshold – adjust as needed

        if (best == null) return null;

        // Scale macros by quantity / 100g
        var scaled = new FoodMatch
        {
            Description = best.Food.Description,
            MatchScore = best.Score,
            Calories = best.Food.Calories * (quantity / 100.0),
            Protein = best.Food.Protein * (quantity / 100.0),
            Carbs = best.Food.Carbs * (quantity / 100.0),
            Fat = best.Food.Fat * (quantity / 100.0)
        };

        return scaled;
    }
}