using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListManager.Models;

public class ListItem
{
    [Key]
    public int ListItemID { get; set; }
    public int? ListID { get; set; }
    public List? List { get; set; }

    [Required]
    [StringLength(150)]
    [Description("Name")]
    public string? ItemName { get; set; }
    public string? ItemDetails { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
    public DateTime AddedDate { get; set; } = DateTime.Now;
}