using RecipeApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FuzzySharp;
using System.Collections.Concurrent;

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
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:11434/api/") };
    private readonly ConcurrentDictionary<string, float[]> _embeddingCache = new();
    private readonly Regex _quantityRegex = new(@"^(\d+(\.\d+)?)\s*g\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NutritionService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "macro.csv");
        if (!File.Exists(path)) return;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var desc = csv.GetField("description")?.Trim() ?? "";
            if (string.IsNullOrEmpty(desc)) continue;

            double.TryParse(csv.GetField("calories"), out var cal);
            double.TryParse(csv.GetField("proteinInGrams"), out var pro);
            double.TryParse(csv.GetField("carbohydratesInGrams"), out var carb);
            double.TryParse(csv.GetField("fatInGrams"), out var fat);

            _foods.Add(new FoodRecord
            {
                Description = desc.ToLower(),
                Calories = cal,
                Protein = pro,
                Carbs = carb,
                Fat = fat
            });
        }
    }

    public async Task ComputeNutritionAsync(Recipe recipe)
    {
        var total = new NutritionProfile();
        recipe.IngredientMatches = new List<FoodMatch>();

        foreach (var ing in recipe.Ingredients)
        {
            var match = await MatchIngredientAsync(ing);
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

    private async Task<FoodMatch?> MatchIngredientAsync(string ingredient)
    {
        ingredient = ingredient.Trim();

        var m = _quantityRegex.Match(ingredient);
        if (!m.Success) return null;

        if (!double.TryParse(m.Groups[1].Value, out var quantity)) return null;

        var name = m.Groups[3].Value.Trim().ToLower();

        // Step 1: Fuzzy pre-filter to top 50 candidates (fast, no Ollama calls)
        var candidates = _foods
            .Select(f => new
            {
                Food = f,
                Score = Fuzz.TokenSetRatio(name, f.Description)
            })
            .OrderByDescending(x => x.Score)
            .Take(50)
            .Select(x => x.Food)
            .ToList();

        if (candidates.Count == 0) return null;

        float[] queryEmb;
        try
        {
            queryEmb = await GetOllamaEmbeddingAsync($"search_query: {name}");
        }
        catch
        {
            return FuzzyMatchIngredient(name, quantity);
        }

        // Step 2: Embed only the 50 candidates (few calls)
        var foodEmbeddings = await Task.WhenAll(candidates.Select(async f => new
        {
            Food = f,
            Embedding = await GetOllamaEmbeddingAsync($"search_document: {f.Description}")
        }));

        var best = foodEmbeddings
            .Select(x => new
            {
                x.Food,
                Score = CosineSimilarity(queryEmb, x.Embedding)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault(x => x.Score >= 0.7);

        if (best == null) return FuzzyMatchIngredient(name, quantity);

        var scale = quantity / 100.0;

        return new FoodMatch
        {
            Description = best.Food.Description,
            MatchScore = (int)(best.Score * 100),
            Calories = best.Food.Calories * scale,
            Protein = best.Food.Protein * scale,
            Carbs = best.Food.Carbs * scale,
            Fat = best.Food.Fat * scale
        };
    }

    private async Task<float[]> GetOllamaEmbeddingAsync(string text)
    {
        if (_embeddingCache.TryGetValue(text, out var cached)) return cached;

        var payload = new { model = "nomic-embed-text", input = text };
        var response = await _http.PostAsJsonAsync("embeddings", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var vector = json!.RootElement
            .GetProperty("embedding")
            .Deserialize<float[]>()!;

        _embeddingCache[text] = vector;
        return vector;
    }

    private FoodMatch? FuzzyMatchIngredient(string name, double quantity)
    {
        var best = _foods
            .Select(f => new
            {
                Food = f,
                Score = Fuzz.TokenSetRatio(name, f.Description)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault(x => x.Score >= 70);

        if (best == null) return null;

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

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}