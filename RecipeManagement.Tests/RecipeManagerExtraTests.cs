using System;
using System.Collections.Generic;
using System.Linq;
using RecipeManagement.Core;

namespace RecipeManagement.Tests;

/// <summary>
/// Additional tests written for Parts A and B. The supplied RecipeManagerTests.cs is left unchanged.
/// Each test follows Arrange / Act / Assert and covers normal behaviour, invalid input,
/// boundary cases or interaction between components.
/// </summary>
public sealed class RecipeManagerExtraTests
{
    // ---------- helpers ----------

    private static Recipe MakeRecipe(
        int id,
        string title,
        string[]? ingredients = null,
        string[]? instructions = null,
        double? protein = null)
    {
        return new Recipe
        {
            Id = id,
            Title = title,
            Ingredients = (ingredients ?? Array.Empty<string>()).ToList(),
            Instructions = (instructions ?? Array.Empty<string>()).ToList(),
            Nutrition = protein is null ? null : new NutritionInfo { ProteinG = protein }
        };
    }

    private static RecipeManager CreateManager()
    {
        return new RecipeManager(new[]
        {
            MakeRecipe(1, "Chicken Curry",
                new[] { "500g chicken", "1 onion", "2 tbsp curry paste" },
                new[] { "Chop onion", "Fry chicken", "Add paste" }, protein: 40),
            MakeRecipe(2, "Vegetable Stir Fry",
                new[] { "1 onion", "2 carrots", "soy sauce" },
                new[] { "Chop vegetables", "Stir fry" }, protein: 8),
            MakeRecipe(3, "Chicken Salad",
                new[] { "200g CHICKEN", "lettuce" },
                new[] { "Mix" }, protein: 40),
            MakeRecipe(4, "Plain Rice",
                new[] { "1 cup rice" },
                Array.Empty<string>(), protein: null),
        });
    }

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesEmptyManager()
    {
        var manager = new RecipeManager(Array.Empty<Recipe>());

        Assert.Equal(0, manager.RecipeCount);
        Assert.Equal(0, manager.ShoppingItemCount);
        Assert.Equal(0, manager.CookingPlanCount);
        Assert.Equal(0, manager.PendingInstructionCount);
        Assert.Equal(0, manager.RemovedRecipeCount);
    }

    [Fact]
    public void Constructor_NullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RecipeManager(null!));
    }

    [Fact]
    public void Constructor_NullRecipeInCollection_ThrowsArgumentException()
    {
        var recipes = new Recipe?[] { MakeRecipe(1, "A"), null };

        Assert.Throws<ArgumentException>(() => new RecipeManager(recipes!));
    }

    [Fact]
    public void Constructor_DuplicateIds_ThrowsArgumentException()
    {
        var recipes = new[] { MakeRecipe(1, "A"), MakeRecipe(1, "B") };

        Assert.Throws<ArgumentException>(() => new RecipeManager(recipes));
    }

    // ---------- Part A: recipe dictionary ----------

    [Fact]
    public void AddRecipe_NewRecipe_ReturnsTrueAndCanBeFound()
    {
        var manager = CreateManager();

        bool added = manager.AddRecipe(MakeRecipe(99, "New Recipe"));

        Assert.True(added);
        Assert.Equal(5, manager.RecipeCount);
        Assert.Equal("New Recipe", manager.FindRecipe(99)?.Title);
    }

    [Fact]
    public void AddRecipe_DuplicateId_ReturnsFalseAndKeepsOriginal()
    {
        var manager = CreateManager();

        bool added = manager.AddRecipe(MakeRecipe(1, "Imposter"));

        Assert.False(added);
        Assert.Equal(4, manager.RecipeCount);
        Assert.Equal("Chicken Curry", manager.FindRecipe(1)?.Title);
    }

    [Fact]
    public void AddRecipe_Null_ReturnsFalse()
    {
        var manager = CreateManager();

        Assert.False(manager.AddRecipe(null!));
        Assert.Equal(4, manager.RecipeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12345)]
    public void FindRecipe_UnknownId_ReturnsNull(int id)
    {
        var manager = CreateManager();

        Assert.Null(manager.FindRecipe(id));
    }

    [Fact]
    public void RemoveRecipe_ExistingRecipe_ReturnsTrueAndRecipeIsGone()
    {
        var manager = CreateManager();

        Assert.True(manager.RemoveRecipe(2));

        Assert.Null(manager.FindRecipe(2));
        Assert.Equal(3, manager.RecipeCount);
    }

    [Fact]
    public void RemoveRecipe_UnknownId_ReturnsFalseAndChangesNothing()
    {
        var manager = CreateManager();

        Assert.False(manager.RemoveRecipe(999));
        Assert.Equal(4, manager.RecipeCount);
    }

    // ---------- Part A: shopping list (List<string>) ----------

    [Fact]
    public void AddIngredientsToShoppingList_AddsAllIngredientsInOrder()
    {
        var manager = CreateManager();

        int added = manager.AddIngredientsToShoppingList(1);

        Assert.Equal(3, added);
        Assert.Equal(3, manager.ShoppingItemCount);
        Assert.Equal(new[] { "500g chicken", "1 onion", "2 tbsp curry paste" }, manager.GetShoppingList());
    }

    [Fact]
    public void AddIngredientsToShoppingList_UnknownRecipe_ReturnsZero()
    {
        var manager = CreateManager();

        Assert.Equal(0, manager.AddIngredientsToShoppingList(999));
        Assert.Equal(0, manager.ShoppingItemCount);
    }

    [Fact]
    public void AddIngredientsToShoppingList_SkipsDuplicatesAcrossRecipes()
    {
        var manager = CreateManager();
        manager.AddIngredientsToShoppingList(1);   // includes "1 onion"

        int added = manager.AddIngredientsToShoppingList(2);   // also "1 onion"

        Assert.Equal(2, added);
        Assert.Equal(5, manager.ShoppingItemCount);
    }

    [Fact]
    public void AddIngredientsToShoppingList_SameRecipeTwice_AddsNothingSecondTime()
    {
        var manager = CreateManager();
        manager.AddIngredientsToShoppingList(1);

        Assert.Equal(0, manager.AddIngredientsToShoppingList(1));
        Assert.Equal(3, manager.ShoppingItemCount);
    }

    [Fact]
    public void AddIngredientsToShoppingList_IgnoresBlankIngredientsAndTrimsText()
    {
        var manager = new RecipeManager(new[]
        {
            MakeRecipe(1, "Messy", new[] { "  flour  ", "", "   ", "sugar" })
        });

        int added = manager.AddIngredientsToShoppingList(1);

        Assert.Equal(2, added);
        Assert.Equal(new[] { "flour", "sugar" }, manager.GetShoppingList());
    }

    [Fact]
    public void AddIngredientsToShoppingList_RecipeWithNoIngredients_ReturnsZero()
    {
        var manager = new RecipeManager(new[] { MakeRecipe(1, "Empty") });

        Assert.Equal(0, manager.AddIngredientsToShoppingList(1));
    }

    [Fact]
    public void ClearShoppingList_EmptiesTheList()
    {
        var manager = CreateManager();
        manager.AddIngredientsToShoppingList(1);

        manager.ClearShoppingList();

        Assert.Equal(0, manager.ShoppingItemCount);
        Assert.Empty(manager.GetShoppingList());
    }

    [Fact]
    public void ClearShoppingList_OnEmptyList_DoesNotThrow()
    {
        var manager = CreateManager();

        var exception = Record.Exception(() => manager.ClearShoppingList());

        Assert.Null(exception);
    }

    [Fact]
    public void GetShoppingList_ReturnsCopy_SoLaterChangesDoNotAffectEarlierResult()
    {
        var manager = CreateManager();
        manager.AddIngredientsToShoppingList(4);
        var snapshot = manager.GetShoppingList();

        manager.AddIngredientsToShoppingList(2);

        Assert.Single(snapshot);
        Assert.Equal(4, manager.ShoppingItemCount);
    }

    // ---------- Part A: cooking plan (LinkedList) ----------

    [Fact]
    public void AddRecipeToCookingPlan_KeepsInsertionOrder()
    {
        var manager = CreateManager();

        manager.AddRecipeToCookingPlan(3);
        manager.AddRecipeToCookingPlan(1);
        manager.AddRecipeToCookingPlan(2);

        Assert.Equal(new[] { 3, 1, 2 }, manager.GetCookingPlan());
        Assert.Equal(3, manager.CookingPlanCount);
    }

    [Fact]
    public void AddRecipeToCookingPlan_UnknownRecipe_ReturnsFalse()
    {
        var manager = CreateManager();

        Assert.False(manager.AddRecipeToCookingPlan(999));
        Assert.Equal(0, manager.CookingPlanCount);
    }

    [Fact]
    public void AddRecipeToCookingPlan_AlreadyPlanned_ReturnsFalse()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);

        Assert.False(manager.AddRecipeToCookingPlan(1));
        Assert.Equal(1, manager.CookingPlanCount);
    }

    [Fact]
    public void RemoveRecipeFromCookingPlan_NotInPlan_ReturnsFalseAndStackUnchanged()
    {
        var manager = CreateManager();

        Assert.False(manager.RemoveRecipeFromCookingPlan(1));
        Assert.Equal(0, manager.RemovedRecipeCount);
        Assert.Null(manager.PeekLastRemovedRecipe());
    }

    [Fact]
    public void RemoveRecipeFromCookingPlan_FromMiddle_KeepsRemainingOrder()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.AddRecipeToCookingPlan(2);
        manager.AddRecipeToCookingPlan(3);

        Assert.True(manager.RemoveRecipeFromCookingPlan(2));

        Assert.Equal(new[] { 1, 3 }, manager.GetCookingPlan());
        Assert.Equal(1, manager.RemovedRecipeCount);
        Assert.Equal(2, manager.PeekLastRemovedRecipe());
    }

    [Fact]
    public void RestoreLastRemovedRecipe_WhenNothingRemoved_ReturnsFalse()
    {
        var manager = CreateManager();

        Assert.False(manager.RestoreLastRemovedRecipe());
        Assert.Empty(manager.GetCookingPlan());
    }

    [Fact]
    public void RestoreLastRemovedRecipe_AddsToEndOfPlanAndShrinksStack()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.AddRecipeToCookingPlan(2);
        manager.RemoveRecipeFromCookingPlan(1);

        Assert.True(manager.RestoreLastRemovedRecipe());

        Assert.Equal(new[] { 2, 1 }, manager.GetCookingPlan());
        Assert.Equal(0, manager.RemovedRecipeCount);
    }

    [Fact]
    public void RestoreLastRemovedRecipe_CalledRepeatedly_RestoresInReverseOrderThenFails()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.AddRecipeToCookingPlan(2);
        manager.AddRecipeToCookingPlan(3);
        manager.RemoveRecipeFromCookingPlan(1);
        manager.RemoveRecipeFromCookingPlan(2);
        manager.RemoveRecipeFromCookingPlan(3);

        Assert.True(manager.RestoreLastRemovedRecipe());   // 3
        Assert.True(manager.RestoreLastRemovedRecipe());   // 2
        Assert.True(manager.RestoreLastRemovedRecipe());   // 1
        Assert.False(manager.RestoreLastRemovedRecipe());

        Assert.Equal(new[] { 3, 2, 1 }, manager.GetCookingPlan());
    }

    [Fact]
    public void RestoreLastRemovedRecipe_RecipeDeletedSinceRemoval_IsSkipped()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.RemoveRecipeFromCookingPlan(1);
        manager.RemoveRecipe(1);   // recipe no longer exists

        Assert.False(manager.RestoreLastRemovedRecipe());
        Assert.Empty(manager.GetCookingPlan());
    }

    [Fact]
    public void PeekLastRemovedRecipe_DoesNotRemoveFromStack()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.RemoveRecipeFromCookingPlan(1);

        Assert.Equal(1, manager.PeekLastRemovedRecipe());
        Assert.Equal(1, manager.PeekLastRemovedRecipe());
        Assert.Equal(1, manager.RemovedRecipeCount);
    }

    // ---------- Part A: cooking instructions (Queue) ----------

    [Fact]
    public void StartCooking_LoadsAllInstructionsIntoQueue()
    {
        var manager = CreateManager();

        Assert.True(manager.StartCooking(1));

        Assert.Equal(3, manager.PendingInstructionCount);
        Assert.Equal("Chop onion", manager.PeekNextInstruction());
    }

    [Fact]
    public void StartCooking_UnknownRecipe_ReturnsFalseAndKeepsCurrentQueue()
    {
        var manager = CreateManager();
        manager.StartCooking(1);

        Assert.False(manager.StartCooking(999));

        Assert.Equal(3, manager.PendingInstructionCount);
        Assert.Equal("Chop onion", manager.PeekNextInstruction());
    }

    [Fact]
    public void StartCooking_AnotherRecipe_ReplacesPendingInstructions()
    {
        var manager = CreateManager();
        manager.StartCooking(1);
        manager.CompleteNextInstruction();

        manager.StartCooking(2);

        Assert.Equal(2, manager.PendingInstructionCount);
        Assert.Equal("Chop vegetables", manager.PeekNextInstruction());
    }

    [Fact]
    public void StartCooking_RecipeWithNoInstructions_ReturnsTrueWithEmptyQueue()
    {
        var manager = CreateManager();

        Assert.True(manager.StartCooking(4));

        Assert.Equal(0, manager.PendingInstructionCount);
        Assert.Null(manager.PeekNextInstruction());
    }

    [Fact]
    public void CompleteNextInstruction_WalksThroughAllStepsThenReturnsNull()
    {
        var manager = CreateManager();
        manager.StartCooking(2);

        Assert.Equal("Chop vegetables", manager.CompleteNextInstruction());
        Assert.Equal("Stir fry", manager.CompleteNextInstruction());
        Assert.Null(manager.CompleteNextInstruction());
        Assert.Equal(0, manager.PendingInstructionCount);
    }

    [Fact]
    public void PeekNextInstruction_BeforeCookingStarts_ReturnsNull()
    {
        var manager = CreateManager();

        Assert.Null(manager.PeekNextInstruction());
        Assert.Null(manager.CompleteNextInstruction());
    }

    [Fact]
    public void PeekNextInstruction_DoesNotConsumeTheStep()
    {
        var manager = CreateManager();
        manager.StartCooking(1);

        manager.PeekNextInstruction();
        manager.PeekNextInstruction();

        Assert.Equal(3, manager.PendingInstructionCount);
    }

    // ---------- Part B: SearchByTitle (LINQ) ----------

    [Fact]
    public void SearchByTitle_IsCaseInsensitiveAndMatchesPartialText()
    {
        var manager = CreateManager();

        var results = manager.SearchByTitle("cHiCkEn");

        Assert.Equal(new[] { 1, 3 }, results.Select(r => r.Id));
    }

    [Fact]
    public void SearchByTitle_TrimsSearchText()
    {
        var manager = CreateManager();

        var results = manager.SearchByTitle("  stir  ");

        Assert.Equal(new[] { 2 }, results.Select(r => r.Id));
    }

    [Fact]
    public void SearchByTitle_NoMatch_ReturnsEmptyList()
    {
        var manager = CreateManager();

        Assert.Empty(manager.SearchByTitle("pizza"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchByTitle_NullOrBlankText_ReturnsEmptyList(string? text)
    {
        var manager = CreateManager();

        Assert.Empty(manager.SearchByTitle(text!));
    }

    [Fact]
    public void SearchByTitle_ResultsAreOrderedById()
    {
        var manager = new RecipeManager(new[]
        {
            MakeRecipe(30, "Soup C"),
            MakeRecipe(10, "Soup A"),
            MakeRecipe(20, "Soup B"),
        });

        var results = manager.SearchByTitle("soup");

        Assert.Equal(new[] { 10, 20, 30 }, results.Select(r => r.Id));
    }

    [Fact]
    public void SearchByTitle_DoesNotChangeTheCatalogue()
    {
        var manager = CreateManager();

        manager.SearchByTitle("chicken");

        Assert.Equal(4, manager.RecipeCount);
    }

    // ---------- Part B: SearchByIngredient (LINQ) ----------

    [Fact]
    public void SearchByIngredient_MatchesPartialTextIgnoringCase()
    {
        var manager = CreateManager();

        var results = manager.SearchByIngredient("chick");

        Assert.Equal(new[] { 1, 3 }, results.Select(r => r.Id));   // "500g chicken" and "200g CHICKEN"
    }

    [Fact]
    public void SearchByIngredient_MatchesAnyIngredientInRecipe()
    {
        var manager = CreateManager();

        var results = manager.SearchByIngredient("onion");

        Assert.Equal(new[] { 1, 2 }, results.Select(r => r.Id));
    }

    [Fact]
    public void SearchByIngredient_NoMatch_ReturnsEmptyList()
    {
        var manager = CreateManager();

        Assert.Empty(manager.SearchByIngredient("truffle"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchByIngredient_NullOrBlankText_ReturnsEmptyList(string? text)
    {
        var manager = CreateManager();

        Assert.Empty(manager.SearchByIngredient(text!));
    }

    [Fact]
    public void SearchByIngredient_RecipeWithNoIngredients_IsNeverReturned()
    {
        var manager = new RecipeManager(new[] { MakeRecipe(1, "Empty") });

        Assert.Empty(manager.SearchByIngredient("anything"));
    }

    // ---------- Part B: GetHighestProteinRecipes (LINQ) ----------

    [Fact]
    public void GetHighestProteinRecipes_ReturnsHighestFirstWithIdTieBreak()
    {
        var manager = CreateManager();   // proteins: 1=40, 2=8, 3=40, 4=none

        var results = manager.GetHighestProteinRecipes(3);

        Assert.Equal(new[] { 1, 3, 2 }, results.Select(r => r.Id));
    }

    [Fact]
    public void GetHighestProteinRecipes_CountLargerThanCatalogue_ReturnsEveryRecipe()
    {
        var manager = CreateManager();

        var results = manager.GetHighestProteinRecipes(100);

        Assert.Equal(4, results.Count);
        Assert.Equal(4, results[^1].Id);   // missing nutrition counts as 0, so it is last
    }

    [Fact]
    public void GetHighestProteinRecipes_MissingNutritionOrProtein_DoesNotThrow()
    {
        var manager = new RecipeManager(new[]
        {
            MakeRecipe(1, "No nutrition", protein: null),
            new Recipe { Id = 2, Title = "Nutrition without protein", Nutrition = new NutritionInfo() },
            MakeRecipe(3, "Has protein", protein: 5),
        });

        var results = manager.GetHighestProteinRecipes(3);

        Assert.Equal(new[] { 3, 1, 2 }, results.Select(r => r.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GetHighestProteinRecipes_ZeroOrNegativeCount_ReturnsEmptyList(int count)
    {
        var manager = CreateManager();

        Assert.Empty(manager.GetHighestProteinRecipes(count));
    }

    [Fact]
    public void GetHighestProteinRecipes_EmptyCatalogue_ReturnsEmptyList()
    {
        var manager = new RecipeManager(Array.Empty<Recipe>());

        Assert.Empty(manager.GetHighestProteinRecipes(5));
    }

    [Fact]
    public void GetHighestProteinRecipes_CountOfOne_ReturnsSingleBestRecipe()
    {
        var manager = CreateManager();

        var results = manager.GetHighestProteinRecipes(1);

        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }

    // ---------- Part B: saved recipes (HashSet) ----------

    [Fact]
    public void AddSavedRecipe_ExistingRecipe_ReturnsTrueAndIsSaved()
    {
        var manager = CreateManager();

        Assert.True(manager.AddSavedRecipe(2));

        Assert.True(manager.IsRecipeSaved(2));
    }

    [Fact]
    public void AddSavedRecipe_UnknownRecipe_ReturnsFalseAndIsNotSaved()
    {
        var manager = CreateManager();

        Assert.False(manager.AddSavedRecipe(999));

        Assert.False(manager.IsRecipeSaved(999));
        Assert.Empty(manager.GetSavedRecipes());
    }

    [Fact]
    public void AddSavedRecipe_Duplicate_ReturnsFalseAndStoresOnlyOnce()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(1);

        Assert.False(manager.AddSavedRecipe(1));

        Assert.Single(manager.GetSavedRecipes());
    }

    [Fact]
    public void RemoveSavedRecipe_SavedRecipe_ReturnsTrueAndIsNoLongerSaved()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(1);

        Assert.True(manager.RemoveSavedRecipe(1));

        Assert.False(manager.IsRecipeSaved(1));
    }

    [Fact]
    public void RemoveSavedRecipe_NotSaved_ReturnsFalse()
    {
        var manager = CreateManager();

        Assert.False(manager.RemoveSavedRecipe(1));
    }

    [Fact]
    public void RemoveSavedRecipe_DoesNotDeleteTheRecipeItself()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(1);

        manager.RemoveSavedRecipe(1);

        Assert.NotNull(manager.FindRecipe(1));
        Assert.Equal(4, manager.RecipeCount);
    }

    [Fact]
    public void IsRecipeSaved_NeverSavedOrUnknown_ReturnsFalse()
    {
        var manager = CreateManager();

        Assert.False(manager.IsRecipeSaved(1));
        Assert.False(manager.IsRecipeSaved(999));
    }

    [Fact]
    public void GetSavedRecipes_ReturnsIdsSortedAscending()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(3);
        manager.AddSavedRecipe(1);
        manager.AddSavedRecipe(2);

        Assert.Equal(new[] { 1, 2, 3 }, manager.GetSavedRecipes());
    }

    [Fact]
    public void GetSavedRecipes_ReturnsCopy_SoLaterChangesDoNotAffectEarlierResult()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(1);
        var snapshot = manager.GetSavedRecipes();

        manager.AddSavedRecipe(2);

        Assert.Single(snapshot);
        Assert.Equal(2, manager.GetSavedRecipes().Count);
    }

    [Fact]
    public void GetSavedRecipes_NothingSaved_ReturnsEmptyList()
    {
        var manager = CreateManager();

        Assert.Empty(manager.GetSavedRecipes());
    }

    // ---------- Component interaction ----------

    [Fact]
    public void RemoveRecipe_AlsoRemovesItFromCookingPlan()
    {
        var manager = CreateManager();
        manager.AddRecipeToCookingPlan(1);
        manager.AddRecipeToCookingPlan(2);

        manager.RemoveRecipe(1);

        Assert.Equal(new[] { 2 }, manager.GetCookingPlan());
    }

    [Fact]
    public void RemoveRecipe_AlsoRemovesItFromSavedRecipes()
    {
        var manager = CreateManager();
        manager.AddSavedRecipe(1);
        manager.AddSavedRecipe(2);

        manager.RemoveRecipe(1);

        Assert.False(manager.IsRecipeSaved(1));
        Assert.Equal(new[] { 2 }, manager.GetSavedRecipes());
    }

    [Fact]
    public void SearchResults_CanBeSavedPlannedAndCookedFromSameCatalogue()
    {
        var manager = CreateManager();

        // Search finds recipes, then every other component uses the same recipe ids.
        var chicken = manager.SearchByIngredient("chicken");
        foreach (var recipe in chicken)
        {
            Assert.True(manager.AddSavedRecipe(recipe.Id));
            Assert.True(manager.AddRecipeToCookingPlan(recipe.Id));
            manager.AddIngredientsToShoppingList(recipe.Id);
        }

        Assert.Equal(new[] { 1, 3 }, manager.GetSavedRecipes());
        Assert.Equal(new[] { 1, 3 }, manager.GetCookingPlan());
        Assert.Equal(5, manager.ShoppingItemCount);   // 3 from recipe 1 + 2 from recipe 3

        Assert.True(manager.StartCooking(manager.GetCookingPlan()[0]));
        Assert.Equal("Chop onion", manager.CompleteNextInstruction());
    }

    [Fact]
    public void AddedRecipe_IsImmediatelyVisibleToSearchesAndProteinReport()
    {
        var manager = CreateManager();
        manager.AddRecipe(MakeRecipe(50, "Protein Shake", new[] { "whey" }, protein: 90));

        Assert.Equal(new[] { 50 }, manager.SearchByTitle("shake").Select(r => r.Id));
        Assert.Equal(new[] { 50 }, manager.SearchByIngredient("whey").Select(r => r.Id));
        Assert.Equal(50, manager.GetHighestProteinRecipes(1)[0].Id);
    }

    [Fact]
    public void RemovedRecipe_NoLongerAppearsInSearchesOrProteinReport()
    {
        var manager = CreateManager();

        manager.RemoveRecipe(1);

        Assert.DoesNotContain(manager.SearchByTitle("curry"), r => r.Id == 1);
        Assert.DoesNotContain(manager.GetHighestProteinRecipes(10), r => r.Id == 1);
    }
}
