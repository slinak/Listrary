using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class Recipe
{
    [Key]
    public int RecipeID { get; set; }

    [Required]
    public string OwnerID { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? SourceUrl { get; set; }

    [StringLength(20000)]
    public string? Instructions { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

    public ICollection<RecipeTag> Tags { get; set; } = new List<RecipeTag>();
}

// A free-form label such as "Chicken", "Mexican" or "Weeknight".
public class RecipeTag
{
    public const int MaxLength = 50;

    [Key]
    public int RecipeTagID { get; set; }

    public int RecipeID { get; set; }
    public Recipe? Recipe { get; set; }

    [Required]
    [StringLength(MaxLength)]
    public string Name { get; set; } = string.Empty;

    // Splits user input like "chicken, #Mexican, Chicken" into distinct, trimmed tag names.
    public static List<string> Parse(string? input) =>
        (input ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim().TrimStart('#').Trim())
            .Where(tag => tag.Length > 0)
            .Select(tag => tag.Length > MaxLength ? tag[..MaxLength].Trim() : tag)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();
}

public class RecipeIngredient
{
    [Key]
    public int RecipeIngredientID { get; set; }

    public int RecipeID { get; set; }
    public Recipe? Recipe { get; set; }

    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    // Optional sub-heading such as "Kebabs" or "Chimichurri" for multi-part recipes.
    [StringLength(100)]
    public string? Section { get; set; }
}
