using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

public class CirclesHelper
{
    public static Func<Circle, string, bool> isCircleOwner = (circle, userId) => circle.OwnerID == userId;

    public static Func<Circle, string, bool> isCircleMember = (circle, userId) => circle.OwnerID == userId || circle.Members.Any(m => m.UserID == userId);
    
    public static Func<CircleInvitation, string, string?, bool> isCorrectRecipient = (invite, userId, userEmail) =>
    {
        return invite.RecipientID == userId ||
            (invite.RecipientID == null && userEmail != null && invite.RecipientEmail != null && invite.RecipientEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
    };
}