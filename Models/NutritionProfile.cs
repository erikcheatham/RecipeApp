namespace RecipeApp.Models;

public class NutritionProfile
{
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }

    public NutritionProfile PerServing(int yield) => new()
    {
        Calories = Calories / yield,
        Protein = Protein / yield,
        Carbs = Carbs / yield,
        Fat = Fat / yield
    };
}

public class FoodMatch
{
    public string Description { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public int MatchScore { get; set; }  // 0-100 fuzzy score
}