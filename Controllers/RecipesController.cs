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
    public async Task<IActionResult> Index()
    {
        var recipes = await _context.Recipes
            .Include(r => r.Ingredients)
            .Where(r => r.OwnerID == CurrentUserId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
        return View(recipes);
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
    public async Task<IActionResult> CreateList(int id, string? listName, ListType listType, int[]? selectedIngredientIds)
    {
        var recipe = await FindOwnedRecipeAsync(id);
        if (recipe == null)
            return NotFound();

        var selected = recipe.Ingredients
            .Where(i => selectedIngredientIds != null && selectedIngredientIds.Contains(i.RecipeIngredientID))
            .OrderBy(i => i.SortOrder)
            .ToList();
        if (selected.Count == 0)
        {
            TempData["RecipeError"] = "Select at least one ingredient.";
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

        var lines = (model.IngredientsText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('-', '*', '\u2022', '\u00B7', '\u2610', '\u25A1', '[', ']', ' ').Trim())
            .Where(line => line.Length > 0)
            .Select(line => line.Length > 500 ? line[..500] : line)
            .Take(200)
            .ToList();
        var instructions = model.Instructions?.Trim();

        var recipe = new Recipe
        {
            OwnerID = CurrentUserId,
            Title = name!,
            Instructions = string.IsNullOrEmpty(instructions) ? null : instructions,
            Ingredients = lines.Select((text, index) => new RecipeIngredient { Text = text, SortOrder = index }).ToList()
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = recipe.RecipeID });
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
            .FirstOrDefaultAsync(r => r.RecipeID == id && r.OwnerID == CurrentUserId);
}
