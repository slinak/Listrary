using ListManager.Models;

namespace ListManager.Dtos;

public static class DtoMappingExtensions
{
    public static ListSummaryDto ToSummaryDto(this List list, string currentUserId, int itemCount)
    {
        return new ListSummaryDto(
            list.ListID,
            list.ListName,
            list.ListType,
            list.OwnerID,
            list.CircleID,
            list.Circle?.Name,
            list.CreatedDate,
            list.LastUpdatedDate,
            list.IsCompleted,
            list.StrictStoreLocations,
            itemCount,
            list.OwnerID == currentUserId);
    }

    public static ListItemDto ToDto(this ListItem item)
    {
        return new ListItemDto(
            item.ListItemID,
            item.ListID ?? 0,
            item.ItemName,
            item.ItemDetails,
            item.StoreLocation,
            item.IsCompleted,
            item.SortOrder,
            item.CreatedDate,
            item.LastUpdatedDate,
            item.AddedDate);
    }

    public static ListShareDto ToDto(this ListShare share)
    {
        return new ListShareDto(
            share.ListShareID,
            share.ListID,
            share.Token,
            share.Permission,
            share.OwnerID,
            share.RecipientID,
            share.CreatedDate,
            share.IsAssigned);
    }

    public static CircleMemberDto ToDto(this CircleMember member)
    {
        return new CircleMemberDto(member.CircleMemberID, member.CircleID, member.UserID, member.JoinedDate);
    }

    public static CircleInvitationDto ToDto(this CircleInvitation invitation)
    {
        return new CircleInvitationDto(
            invitation.CircleInvitationID,
            invitation.CircleID,
            invitation.InviterID,
            invitation.RecipientEmail,
            invitation.RecipientID,
            invitation.Token,
            invitation.Accepted,
            invitation.CreatedDate);
    }

    public static CircleDto ToDto(this Circle circle)
    {
        return new CircleDto(
            circle.CircleID,
            circle.Name,
            circle.OwnerID,
            circle.CreatedDate,
            circle.Members.Select(m => m.ToDto()).ToList(),
            circle.Invitations.Select(i => i.ToDto()).ToList());
    }
}
