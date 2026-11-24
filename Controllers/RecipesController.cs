using System.Security.Claims;
using ListManager.Data;
using ListManager.Models;
using ListManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListManager.Controllers;

[Authorize]
public class RecipesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRecipeImportService _importService;

    public RecipesController(ApplicationDbContext context, IRecipeImportService importService)
    {
        _context = context;
        _importService = importService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Index(string? tag)
    {
        // Filtering happens in the browser; ?tag= only picks the starting filter pill.
        var allTags = await _context.RecipeTags
            .Where(t => t.Recipe!.OwnerID == CurrentUserId)
            .Select(t => t.Name)
            .ToListAsync();
        var selectedTag = RecipeTag.Parse(tag).FirstOrDefault();

        // The most-used tags become filter pills; the search box finds the rest.
        var pillTags = allTags
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .Take(8)
            .ToList();
        if (selectedTag != null && !pillTags.Contains(selectedTag, StringComparer.OrdinalIgnoreCase))
            pillTags.Add(selectedTag);

        ViewBag.PillTags = pillTags;
        ViewBag.SelectedTag = selectedTag;

        var recipes = await _context.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Tags.OrderBy(t => t.Name))
            .AsSplitQuery()
            .Where(r => r.OwnerID == CurrentUserId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return View(recipes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTags(int id, string? tags)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        AddNewTags(recipe, RecipeTag.Parse(tags));
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTag(int id, int tagId)
    {
        var tag = await _context.RecipeTags
            .FirstOrDefaultAsync(t => t.RecipeTagID == tagId && t.RecipeID == id && t.Recipe!.OwnerID == CurrentUserId);
        if (tag != null)
        {
            _context.RecipeTags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // Skips names the recipe already has (ignoring case) and caps the total per recipe.
    private static void AddNewTags(Recipe recipe, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (recipe.Tags.Count >= 30)
                break;
            if (recipe.Tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            recipe.Tags.Add(new RecipeTag { Name = name });
        }
    }

    // Imported recipes are always saved to the current user's recipe collection.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string? url, CancellationToken cancellationToken)
    {
        var trimmedUrl = url?.Trim();
        if (string.IsNullOrEmpty(trimmedUrl) || !Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri))
            return ImportFailed(trimmedUrl, RecipeImportService.InvalidUrlMessage);

        // Same page with a different #fragment is the same recipe.
        var sourceUrl = uri.GetLeftPart(UriPartial.Query);
        if (sourceUrl.Length > 2000)
            sourceUrl = sourceUrl[..2000];

        var existing = await _context.Recipes
            .FirstOrDefaultAsync(r => r.OwnerID == CurrentUserId && (r.SourceUrl == sourceUrl || r.SourceUrl == trimmedUrl), cancellationToken);
        if (existing != null)
            return RedirectToAction(nameof(Details), new { id = existing.RecipeID });

        var result = await _importService.ImportFromUrlAsync(trimmedUrl, cancellationToken);
        if (!result.Succeeded)
            return ImportFailed(trimmedUrl, result.Error!);

        var recipe = new Recipe
        {
            OwnerID = CurrentUserId,
            Title = result.Title ?? uri.Host,
            SourceUrl = sourceUrl,
            Instructions = result.Instructions,
            Ingredients = result.Ingredients
                .Select((text, index) => new RecipeIngredient { Text = text, SortOrder = index })
                .ToList()
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = recipe.RecipeID });
    }

    private IActionResult ImportFailed(string? url, string error)
    {
        TempData["RecipeError"] = error;
        TempData["RecipeUrl"] = url;
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        return recipe == null ? NotFound() : View(recipe);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateList(int id, string? listName, ListType listType)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        var selected = recipe.Ingredients.OrderBy(i => i.SortOrder).ToList();
        if (selected.Count == 0)
        {
            TempData["RecipeError"] = "This recipe has no ingredients.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var name = string.IsNullOrWhiteSpace(listName) ? recipe.Title : listName.Trim();
        var now = DateTime.Now;
        var list = new List
        {
            ListName = name.Length > 100 ? name[..100] : name,
            OwnerID = CurrentUserId,
            ListType = listType,
            CreatedDate = now,
            LastUpdatedDate = now,
            Items = selected.Select((ingredient, index) => new ListItem
            {
                ItemName = ingredient.Text.Length > 150 ? ingredient.Text[..150] : ingredient.Text,
                ItemDetails = ingredient.Text.Length > 150 ? ingredient.Text : null,
                SortOrder = index,
                CreatedDate = now,
                LastUpdatedDate = now,
                AddedDate = now
            }).ToList()
        };
        _context.Lists.Add(list);
        await _context.SaveChangesAsync();

        return RedirectToAction("Manage", "Lists", new { id = list.ListID });
    }

    [HttpGet]
    public IActionResult Create() => View(new RecipeFormModel());

    // Ingredients and instructions may come from OCR text the user has reviewed in the browser.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeFormModel model)
    {
        var name = model.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            ModelState.AddModelError(nameof(model.Name), "Recipe name is required.");
        if (!ModelState.IsValid)
            return View(model);

        var instructions = model.Instructions?.Trim();
        var recipe = new Recipe
        {
            OwnerID = CurrentUserId,
            Title = name!,
            Instructions = string.IsNullOrEmpty(instructions) ? null : instructions,
            Ingredients = ParseIngredients(model.IngredientsText)
        };
        AddNewTags(recipe, RecipeTag.Parse(model.Tags));
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = recipe.RecipeID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        ViewBag.RecipeID = recipe.RecipeID;
        return View(new RecipeFormModel
        {
            Name = recipe.Title,
            IngredientsText = FormatIngredients(recipe.Ingredients),
            Instructions = recipe.Instructions
        });
    }

    // Replaces the recipe's name, ingredients and instructions. Tags are edited on the Details page.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RecipeFormModel model)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        var name = model.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            ModelState.AddModelError(nameof(model.Name), "Recipe name is required.");
        if (!ModelState.IsValid)
        {
            ViewBag.RecipeID = recipe.RecipeID;
            return View(model);
        }

        var instructions = model.Instructions?.Trim();
        recipe.Title = name!;
        recipe.Instructions = string.IsNullOrEmpty(instructions) ? null : instructions;
        _context.RecipeIngredients.RemoveRange(recipe.Ingredients);
        foreach (var ingredient in ParseIngredients(model.IngredientsText))
            recipe.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // One ingredient per line. A line ending in a colon (e.g. "Kebabs:") starts a new ingredient section.
    private static List<RecipeIngredient> ParseIngredients(string? ingredientsText)
    {
        var lines = (ingredientsText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        var ingredients = new List<RecipeIngredient>();
        string? currentSection = null;
        foreach (var line in lines)
        {
            if (line.EndsWith(':'))
            {
                var heading = line.TrimEnd(':').Trim();
                currentSection = string.IsNullOrEmpty(heading) ? null : heading[..Math.Min(heading.Length, 100)];
                continue;
            }

            var text = line.TrimStart('-', '*', '\u2022', '\u00B7', '\u2610', '\u25A1', '[', ']', ' ').Trim();
            if (text.Length == 0)
                continue;
            if (ingredients.Count >= 200)
                break;

            ingredients.Add(new RecipeIngredient
            {
                Text = text.Length > 500 ? text[..500] : text,
                Section = currentSection,
                SortOrder = ingredients.Count
            });
        }
        return ingredients;
    }

    // The reverse of ParseIngredients, so editing round-trips sections as "Heading:" lines.
    private static string FormatIngredients(IEnumerable<RecipeIngredient> ingredients)
    {
        var lines = new List<string>();
        string? currentSection = null;
        foreach (var ingredient in ingredients.OrderBy(i => i.SortOrder))
        {
            var section = string.IsNullOrWhiteSpace(ingredient.Section) ? null : ingredient.Section.Trim();
            if (section != null && !string.Equals(section, currentSection, StringComparison.Ordinal))
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);
                lines.Add(section.TrimEnd(':') + ":");
            }
            currentSection = section;
            lines.Add(ingredient.Text.EndsWith(':') ? ingredient.Text.TrimEnd(':') : ingredient.Text);
        }
        return string.Join("\n", lines);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<Recipe?> FindOwnedRecipeAsync(int id) =>
        _context.Recipes
            .Include(r => r.Ingredients.OrderBy(i => i.SortOrder))
            .Include(r => r.Tags.OrderBy(t => t.Name))
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.RecipeID == id && r.OwnerID == CurrentUserId);
}
