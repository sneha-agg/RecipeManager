using System;
using System.Collections.Generic;
using System.Linq;

namespace RecipeManagement.Core;

/// <summary>
/// Part A uses five collections: Dictionary&lt;int, Recipe&gt;, List&lt;string&gt;,
/// LinkedList&lt;int&gt;, Stack&lt;int&gt; and Queue&lt;string&gt;.
///
/// ASSUMED Recipe members (rename to match your starter's Recipe class):
///   int Id, IEnumerable&lt;string&gt; Ingredients, IEnumerable&lt;string&gt; Instructions.
/// </summary>
public sealed class RecipeManager : IRecipeManager
{
    private readonly Dictionary<int, Recipe> recipes = new();
    private readonly List<string> shoppingList = new();
    private readonly LinkedList<int> cookingPlan = new();
    private readonly Stack<int> removedRecipes = new();      // ids removed from the cooking plan
    private readonly Queue<string> pendingInstructions = new();

    public RecipeManager(IEnumerable<Recipe> recipes)
    {
        ArgumentNullException.ThrowIfNull(recipes);

        foreach (var recipe in recipes)
        {
            if (recipe is null)
                throw new ArgumentException("Recipe collection contains a null recipe.", nameof(recipes));

            // 'this.recipes' because the constructor parameter shadows the field.
            if (!this.recipes.TryAdd(recipe.Id, recipe))
                throw new ArgumentException($"Duplicate recipe id {recipe.Id}.", nameof(recipes));
        }
    }

    public int RecipeCount => recipes.Count;
    public int ShoppingItemCount => shoppingList.Count;
    public int CookingPlanCount => cookingPlan.Count;
    public int PendingInstructionCount => pendingInstructions.Count;
    public int RemovedRecipeCount => removedRecipes.Count;

    // ---------------- Part A ----------------

    public bool AddRecipe(Recipe recipe)
    {
        if (recipe is null) return false;
        return recipes.TryAdd(recipe.Id, recipe);
    }

    public Recipe? FindRecipe(int recipeId) =>
        recipes.TryGetValue(recipeId, out var recipe) ? recipe : null;

    public bool RemoveRecipe(int recipeId)
    {
        if (!recipes.Remove(recipeId)) return false;

        // Keep the cooking plan consistent with the dictionary.
        cookingPlan.Remove(recipeId);
        return true;
    }

    public int AddIngredientsToShoppingList(int recipeId)
    {
        var recipe = FindRecipe(recipeId);
        if (recipe is null) return 0;

        int added = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient)) continue;

            var item = ingredient.Trim();
            if (shoppingList.Contains(item, StringComparer.OrdinalIgnoreCase)) continue;

            shoppingList.Add(item);
            added++;
        }
        return added;
    }

    public IReadOnlyList<string> GetShoppingList() => shoppingList.ToList().AsReadOnly();

    public void ClearShoppingList() => shoppingList.Clear();

    public bool AddRecipeToCookingPlan(int recipeId)
    {
        if (!recipes.ContainsKey(recipeId)) return false;
        if (cookingPlan.Contains(recipeId)) return false;

        cookingPlan.AddLast(recipeId);
        return true;
    }

    public bool RemoveRecipeFromCookingPlan(int recipeId)
    {
        if (!cookingPlan.Remove(recipeId)) return false;

        removedRecipes.Push(recipeId);   // so it can be restored (LIFO)
        return true;
    }

    public bool RestoreLastRemovedRecipe()
    {
        // Discard ids whose recipe was deleted since, or that are already back in the plan.
        while (removedRecipes.Count > 0)
        {
            int id = removedRecipes.Pop();
            if (recipes.ContainsKey(id) && !cookingPlan.Contains(id))
            {
                cookingPlan.AddLast(id);
                return true;
            }
        }
        return false;
    }

    public int? PeekLastRemovedRecipe() =>
        removedRecipes.TryPeek(out int id) ? id : null;

    public IReadOnlyList<int> GetCookingPlan() => cookingPlan.ToList().AsReadOnly();

    public bool StartCooking(int recipeId)
    {
        var recipe = FindRecipe(recipeId);
        if (recipe is null) return false;

        pendingInstructions.Clear();
        foreach (var step in recipe.Instructions)
            pendingInstructions.Enqueue(step);

        return true;
    }

    public string? PeekNextInstruction() =>
        pendingInstructions.TryPeek(out var step) ? step : null;

    public string? CompleteNextInstruction() =>
        pendingInstructions.TryDequeue(out var step) ? step : null;

    // ---------------- Part B (not started) ----------------

    public IReadOnlyList<Recipe> SearchByTitle(string searchText) =>
        throw new NotImplementedException("Part B: implement SearchByTitle.");

    public IReadOnlyList<Recipe> SearchByIngredient(string searchText) =>
        throw new NotImplementedException("Part B: implement SearchByIngredient.");

    public IReadOnlyList<Recipe> GetHighestProteinRecipes(int count) =>
        throw new NotImplementedException("Part B: implement GetHighestProteinRecipes.");

    public bool AddSavedRecipe(int recipeId) =>
        throw new NotImplementedException("Part B: implement AddSavedRecipe.");

    public bool RemoveSavedRecipe(int recipeId) =>
        throw new NotImplementedException("Part B: implement RemoveSavedRecipe.");

    public bool IsRecipeSaved(int recipeId) =>
        throw new NotImplementedException("Part B: implement IsRecipeSaved.");

    public IReadOnlyList<int> GetSavedRecipes() =>
        throw new NotImplementedException("Part B: implement GetSavedRecipes.");
}