using ListManager.Models;

namespace ListManager.Services;

/// <summary>
/// Shared list-item business logic (store-location normalization/matching, item-name
/// normalization, and suggestion queries) used by both the MVC controllers and the
/// JSON API controllers so the two surfaces never drift out of sync.
/// </summary>
public interface IListItemService
{
    /// <summary>
    /// Orders items the way the "manage" view does: incomplete items first (grouped by
    /// store location, items without a location first), then completed items, each
    /// group ordered by SortOrder.
    /// </summary>
    List<ListItem> GetManageViewOrderedItems(IEnumerable<ListItem> items);

    string NormalizeItemName(string? value);

    string CanonicalizeStoreLocation(string? value);

    /// <summary>
    /// Resolves (or creates) the StoreLocationOption for a raw store-location string
    /// entered for an item, returning a warning (similar option existed) or an error
    /// (strict mode violation) as appropriate. Does not call SaveChanges; the caller
    /// is responsible for persisting any added entity.
    /// </summary>
    (StoreLocationOption? option, string? warning, string? error) ResolveStoreLocationOption(int listId, string? rawStoreLocation, bool strictStoreLocations);

    /// <summary>
    /// Records/updates the learned preferred store location for an item name. Does not
    /// call SaveChanges; the caller is responsible for persisting.
    /// </summary>
    void LearnItemLocationPreference(int listId, string? itemName, int storeLocationOptionId);

    List<string> GetItemNameSuggestions(int listId, string? query);

    List<string> GetStoreLocationSuggestions(int listId, string? itemName, string? query);
}
