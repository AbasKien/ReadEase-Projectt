using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Area("Mobile")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET Areas/Mobile/api/Notifications ───────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifs = await _notifications.GetForUserAsync(UserId);
        return Ok(notifs);
    }

    // ── GET Areas/Mobile/api/Notifications/unread-count ──────────────────
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notifications.GetUnreadCountAsync(UserId);
        return Ok(new { unreadCount = count });
    }

    // ── POST Areas/Mobile/api/Notifications/mark-all-read ────────────────
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(UserId);
        return Ok(new { message = "All notifications marked as read." });
    }

    // ── POST Areas/Mobile/api/Notifications/mark-read/{id} ───────────────
    [HttpPost("mark-read/{id:guid}")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notifications.MarkReadAsync(id);
        return Ok(new { message = "Notification marked as read." });
    }
}