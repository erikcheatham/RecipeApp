using Microsoft.AspNetCore.Components;
using RecipeApp.Components;
using RecipeApp.Models;
using RecipeApp.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<RecipeService>();
builder.Services.AddSingleton<RecipesFromJSONService>();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

builder.Services.AddScoped<RecipesFromWebService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/recipes", async (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.ContentRootPath, "Data", "recipes.json");

    if (!File.Exists(path))
    {
        return Results.NotFound();
    }

    var json = await File.ReadAllTextAsync(path);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var loaded = JsonSerializer.Deserialize<List<Recipe>>(json, options) ?? new();

    var response = new RecipeListResponse
    {
        Recipes = loaded,
        TotalCount = loaded.Count
    };

    return Results.Json(response);
});

app.MapPost("/api/recipes", async (Recipe recipe, IWebHostEnvironment env) =>
{
    var list = await LoadRecipesAsync(env);

    if (list.Any(r => r.Title.Equals(recipe.Title, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict("A recipe with this title already exists.");
    }

    list.Add(recipe);
    await SaveRecipesAsync(list, env);
    return Results.Created("/api/recipes", recipe);
});

app.MapPut("/api/recipes/{title}", async (string title, Recipe updatedRecipe, IWebHostEnvironment env) =>
{
    var list = await LoadRecipesAsync(env);
    var existing = list.FirstOrDefault(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    if (existing == null)
    {
        return Results.NotFound();
    }

    var newTitle = updatedRecipe.Title;
    if (!newTitle.Equals(title, StringComparison.OrdinalIgnoreCase) &&
        list.Any(r => r.Title.Equals(newTitle, StringComparison.OrdinalIgnoreCase) && !r.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict("A recipe with the new title already exists.");
    }

    existing.Title = newTitle;
    existing.Yield = updatedRecipe.Yield;
    existing.Ingredients.Clear();
    existing.Ingredients.AddRange(updatedRecipe.Ingredients);

    await SaveRecipesAsync(list, env);
    return Results.Ok(existing);
});

app.MapDelete("/api/recipes/{title}", async (string title, IWebHostEnvironment env) =>
{
    var list = await LoadRecipesAsync(env);
    var removed = list.RemoveAll(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

    if (removed == 0)
    {
        return Results.NotFound();
    }

    await SaveRecipesAsync(list, env);
    return Results.NoContent();
});

// Helper methods
static async Task<List<Recipe>> LoadRecipesAsync(IWebHostEnvironment env)
{
    var path = Path.Combine(env.ContentRootPath, "Data", "recipes.json");
    if (!File.Exists(path)) return new List<Recipe>();

    var json = await File.ReadAllTextAsync(path);
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    return JsonSerializer.Deserialize<List<Recipe>>(json, options) ?? new List<Recipe>();
}

static async Task SaveRecipesAsync(List<Recipe> recipes, IWebHostEnvironment env)
{
    var path = Path.Combine(env.ContentRootPath, "Data", "recipes.json");
    var options = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(recipes, options);
    await File.WriteAllTextAsync(path, json);
}

app.Run();
