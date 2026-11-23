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
}
