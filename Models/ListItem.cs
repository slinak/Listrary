using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListManager.Models;

public class ListItem
{
    [Key]
    public int ListItemID { get; set; }
    [ForeignKey("ListID")]
    public int? ListID { get; set; }
    public string? ItemName { get; set; }
    public string? ItemDetails { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime AddedDate { get; set; }
}