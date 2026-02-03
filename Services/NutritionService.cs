using CsvHelper;
using CsvHelper.Configuration;
using RecipeApp.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
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
    private readonly HttpClient _http = new();
    private readonly Dictionary<string, float[]> _embeddingCache = new(); // Cache for performance
    private readonly Regex _quantityRegex = new(@"^(\d+(\.\d+)?)\s*g\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Replace with your actual xAI API key (store securely, e.g., in appsettings or user secrets)
    private const string ApiKey = "api-key";

    // Current xAI embeddings model (check https://x.ai/api for latest – as of 2025, often "grok-embedding" or similar)
    private const string EmbeddingModel = "grok-4";

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

        _http.BaseAddress = new Uri("https://api.x.ai/v1/");
        _http.DefaultRequestHeaders.Clear(); // Safety
        _http.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

        // Get embedding for query
        var queryEmb = await GetEmbeddingAsync(name);

        // Compute similarities (async for cache)
        var matches = await Task.WhenAll(_foods.Select(async f => new
        {
            Food = f,
            Score = CosineSimilarity(queryEmb, await GetEmbeddingAsync(f.Description))
        }));

        var best = matches
            .Where(x => x.Score >= 0.75) // Semantic threshold (higher than fuzzy)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best == null) return null;

        var scale = quantity / 100.0;

        return new FoodMatch
        {
            Description = best.Food.Description,
            MatchScore = (int)(best.Score * 100), // Convert to 0-100
            Calories = best.Food.Calories * scale,
            Protein = best.Food.Protein * scale,
            Carbs = best.Food.Carbs * scale,
            Fat = best.Food.Fat * scale
        };
    }

    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_embeddingCache.TryGetValue(text, out var cached)) return cached;

        var payload = new
        {
            model = EmbeddingModel,
            input = text
        };

        var response = await _http.PostAsJsonAsync("embeddings", payload);
        if (!response.IsSuccessStatusCode)
        {
            // Fallback or error handling
            throw new Exception($"Embedding API error: {await response.Content.ReadAsStringAsync()}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var vector = json!.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .Deserialize<float[]>()!;

        _embeddingCache[text] = vector;
        return vector;
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