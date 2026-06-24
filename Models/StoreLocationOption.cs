using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class StoreLocationOption
{
    [Key]
    public int StoreLocationOptionID { get; set; }

    public int ListID { get; set; }
    public List? List { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string NormalizedName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUsedDate { get; set; } = DateTime.Now;

    public ICollection<ListItem> ListItems { get; set; } = new List<ListItem>();
}
