using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class ItemLocationPreference
{
    [Key]
    public int ItemLocationPreferenceID { get; set; }

    public int ListID { get; set; }
    public List? List { get; set; }

    [Required]
    [StringLength(150)]
    public string ItemNameNormalized { get; set; } = string.Empty;

    public int StoreLocationOptionID { get; set; }
    public StoreLocationOption? StoreLocationOption { get; set; }

    public int UsageCount { get; set; } = 1;
    public DateTime LastUsedDate { get; set; } = DateTime.Now;
}
