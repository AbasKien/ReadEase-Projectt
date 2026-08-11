using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services;
using ReadEase.ViewModels.Notification;
using System.Security.Claims;
using ReadEase.Services.Interfaces;

namespace ReadEase.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notifService;

    public NotificationController(INotificationService notifService)
    {
        _notifService = notifService;
    }

    // GET /Notification/GetDropdown  (AJAX - initial load)
    [HttpGet]
    public async Task<IActionResult> GetDropdown()
    {
        var userId = GetUserId();
        var notifs = await _notifService.GetForUserAsync(userId);
        var vms = notifs.Select(n => new NotificationViewModel
        {
            Id = n.Id,
            Message = n.Message,
            Link = n.Link,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
        return Json(vms);
    }

    // GET /Notification/UnreadCount (AJAX badge refresh)
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _notifService.GetUnreadCountAsync(GetUserId());
        return Json(new { count });
    }

    // POST /Notification/MarkRead/{id}
    [HttpPost]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notifService.MarkReadAsync(id);
        var count = await _notifService.GetUnreadCountAsync(GetUserId());
        return Json(new { success = true, unreadCount = count });
    }

    // POST /Notification/MarkAllRead
    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifService.MarkAllReadAsync(GetUserId());
        return Json(new { success = true });
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Guid.Empty.ToString());
}