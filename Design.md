# Design

## 1. Saved recipe collection

I used a `HashSet<int>.Add` to store the recipe id's.

- **Add:** `HashSet<int>.Add` will add the id. If it already exists it will return `false`. This means we can avoid duplication.
- **Remove:**: `HashSet<int>.Remove` method works by removing the value, if the id doesn't exist it returns false. This also means the return of RemoveSavedRecipe is directly taken from this method.
- **Membership:** `Contains` is hashed meaning `IsRecipeSaved` will not have to iterate through the collection.
- **Why not a `List<int>`:** A list will allow duplication. We would have to check `Contains` first which uses a linear search. This will cause issues when adding and checking if something exists.

As there is no guaranteed ordering in a hashset `GetSavedRecipes` will sort the `recipes` in ascending order. The list is also returned as a copy.

## 2. Integration

The part B functionality uses the Part A recipe catalogue `(Dictionary<int, Recipe>)` within the same `RecipeManager`. The saved recipes only store recipe ids. `AddSavedRecipe` makes sure that you can only add recipes that exist by checking against the dictionary. `SearchByTitle`, `SearchByIngredient` and `GetHighestProteinRecipes` all use LINQ on `recipes.Values` to ensure that they are using the same catalogue from Part A. To make sure that the results are predictable they are ordered by recipe id, if two recipes have the same protein then it will be ordered by id. This is because there is no guarantee on the order of items in a dictionary. Recipes without any nutritional information will be considered to have 0g of protein.

## 3. Basic complexity

| Operation                       | Structure         | Expected complexity | Reason                                                                             |
| ------------------------------- | ----------------- | ------------------- | ---------------------------------------------------------------------------------- |
| Lookup recipe by ID             | Dictionary        | Average O(1)        | Hash-based key lookup.                                                             |
| Traverse cooking plan           | LinkedList        | O(n)                | Each planned recipe may need to be visited.                                        |
| Complete next instruction       | Queue             | O(1)                | The item at the front is removed.                                                  |
| LINQ title/ingredient search    | Recipe collection | O(n)                | Each recipe may need to be inspected.                                              |
| Check whether a recipe is saved | HashSet           | Average O(1)        | `Contains` hashes the id and checks one bucket instead of scanning every saved id. |
