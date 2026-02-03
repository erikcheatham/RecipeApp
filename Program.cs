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

app.Run();
