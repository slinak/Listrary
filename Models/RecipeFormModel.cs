using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class RecipeFormModel
{
    [Required(ErrorMessage = "Recipe name is required.")]
    [StringLength(200)]
    public string? Name { get; set; }

    public string? IngredientsText { get; set; }

    [StringLength(20000)]
    public string? Instructions { get; set; }
}
