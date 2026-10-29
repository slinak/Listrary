using ListManager.Models;

namespace ListManager.Dtos;

public record CircleSummaryDto(int CircleId, string Name);

public record ListSummaryDto(
    int ListId,
    string? ListName,
    ListType? ListType,
    string? OwnerId,
    int? CircleId,
    string? CircleName,
    DateTime CreatedDate,
    DateTime LastUpdatedDate,
    bool IsCompleted,
    bool StrictStoreLocations,
    int ItemCount,
    bool IsOwner);

public record ListItemDto(
    int ListItemId,
    int ListId,
    string? ItemName,
    string? ItemDetails,
    string? StoreLocation,
    bool IsCompleted,
    int SortOrder,
    DateTime CreatedDate,
    DateTime LastUpdatedDate,
    DateTime AddedDate);

public record ListShareDto(
    int ListShareId,
    int ListId,
    string Token,
    ListSharePermission Permission,
    string OwnerId,
    string? RecipientId,
    DateTime CreatedDate,
    bool IsAssigned);

public record ListDetailDto(
    ListSummaryDto List,
    List<ListItemDto> Items,
    bool CanEdit,
    string? ShareToken,
    List<ListShareDto> ActiveShares);

public record CircleMemberDto(int CircleMemberId, int CircleId, string UserId, DateTime JoinedDate);

public record CircleInvitationDto(
    int CircleInvitationId,
    int CircleId,
    string InviterId,
    string? RecipientEmail,
    string? RecipientId,
    string Token,
    bool Accepted,
    DateTime CreatedDate);

public record CircleDto(
    int CircleId,
    string Name,
    string OwnerId,
    DateTime CreatedDate,
    List<CircleMemberDto> Members,
    List<CircleInvitationDto> Invitations);

public record CirclesIndexDto(List<CircleDto> Circles, List<CircleInvitationDto> PendingInvitations);

/// <summary>
/// Request payload for creating or updating a <see cref="Models.List"/>.
/// </summary>
public class ListRequest
{
    public string? ListName { get; set; }
    public ListType? ListType { get; set; }
    public int? CircleId { get; set; }
    public bool StrictStoreLocations { get; set; }
}

/// <summary>
/// Request payload for creating or updating a <see cref="Models.ListItem"/>.
/// </summary>
public class ListItemRequest
{
    public string? ItemName { get; set; }
    public string? ItemDetails { get; set; }
    public string? StoreLocation { get; set; }
    public bool IsCompleted { get; set; }
}

public class ShareRequest
{
    public ListSharePermission Permission { get; set; }
}

public record ShareResultDto(ListShareDto ListShare, string? ShareLink);

public record ListItemResultDto(ListItemDto Item, string? Warning);

public class CreateCircleRequest
{
    public string Name { get; set; } = string.Empty;
}

public class InviteRequest
{
    public string? RecipientEmail { get; set; }
    public string? RecipientId { get; set; }
}
