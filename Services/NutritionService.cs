using RecipeApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using FuzzySharp;
using System.Text.RegularExpressions;

namespace RecipeApp.Services;

public class FoodRecord
{
    public string Description { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
}

public class NutritionService
{
    private readonly List<FoodRecord> _foods = new();
    private readonly Regex _quantityRegex = new(@"^(\d+(\.\d+)?)\s*g\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NutritionService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "macro.csv");
        if (!File.Exists(path)) return;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null // Skip bad rows silently
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var desc = csv.GetField("description")?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(desc)) continue;

            double.TryParse(csv.GetField("calories"), out var cal);
            double.TryParse(csv.GetField("proteinInGrams"), out var pro);
            double.TryParse(csv.GetField("carbohydratesInGrams"), out var carb);
            double.TryParse(csv.GetField("fatInGrams"), out var fat);

            _foods.Add(new FoodRecord
            {
                Description = desc,
                Calories = cal,
                Protein = pro,
                Carbs = carb,
                Fat = fat
            });
        }
    }

    public void ComputeNutrition(Recipe recipe)
    {
        var total = new NutritionProfile();
        recipe.IngredientMatches = new List<FoodMatch>();

        foreach (var ing in recipe.Ingredients)
        {
            var match = MatchIngredient(ing);
            if (match != null)
            {
                recipe.IngredientMatches.Add(match);
                total.Calories += match.Calories;
                total.Protein += match.Protein;
                total.Carbs += match.Carbs;
                total.Fat += match.Fat;
            }
        }

        recipe.TotalNutrition = total;
        recipe.PerServingNutrition = total.PerServing(recipe.Yield > 0 ? recipe.Yield : 1);
    }

    private FoodMatch? MatchIngredient(string ingredient)
    {
        ingredient = ingredient.Trim();

        // Parse quantity in grams (assumes all ingredients use "g" – matches your data format)
        var m = _quantityRegex.Match(ingredient);
        if (!m.Success) return null;

        if (!double.TryParse(m.Groups[1].Value, out var quantity)) return null;

        var name = m.Groups[3].Value.Trim().ToLower();

        // Fuzzy match using TokenSetRatio (best for ingredient names with variations)
        var best = _foods
            .Select(f => new
            {
                Food = f,
                Score = Fuzz.TokenSetRatio(name, f.Description)
            })
            .Where(x => x.Score >= 70) // Adjustable threshold
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best == null) return null;

        // Scale macros per 100g to actual quantity
        var scale = quantity / 100.0;

        return new FoodMatch
        {
            Description = best.Food.Description,
            MatchScore = best.Score,
            Calories = best.Food.Calories * scale,
            Protein = best.Food.Protein * scale,
            Carbs = best.Food.Carbs * scale,
            Fat = best.Food.Fat * scale
        };
    }
}