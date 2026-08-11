using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Enums;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Subscribe;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subService;
    private readonly IWebHostEnvironment _env;

    public SubscriptionController(ISubscriptionService subService, IWebHostEnvironment env)
    {
        _subService = subService;
        _env = env;
    }

    // GET /Subscription/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var latest = await _subService.GetStatusAsync(userId);

        // Read the user's actual current tier from the DB so we know
        // whether they are Free / Premium / PremiumPlus right now.
        var currentTier = await _subService.GetCurrentTierAsync(userId);

        var vm = new SubscribeViewModel
        {
            CurrentStatus = latest?.Status,
            RejectionReason = latest?.Reason,
            SubmittedAt = latest?.SubmittedAt,
            // What the latest REQUEST was for (or default Premium for new users)
            RequestedTier = latest?.RequestedTier ?? SubscriptionTier.Premium,
            // Actual tier the user holds right now
            CurrentTier = currentTier,
            IsSubscribed = currentTier != SubscriptionTier.Free
        };
        return View("~/Views/Subscription/Subscribe.cshtml", vm);
    }

    // POST /Subscription/Submit
    [HttpPost]
    public async Task<IActionResult> Submit(IFormFile? receiptPhoto, SubscriptionTier requestedTier)
    {
        // Validate tier selection
        if (requestedTier == SubscriptionTier.Free)
        {
            TempData["ErrorMessage"] = "Please select a valid subscription tier.";
            return RedirectToAction(nameof(Index));
        }

        string? receiptPath = null;

        if (receiptPhoto is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(receiptPhoto.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG or WEBP images are allowed.";
                return RedirectToAction(nameof(Index));
            }
            var uploads = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploads);
            var fileName = $"{GetUserId()}_{DateTime.UtcNow.Ticks}{ext}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await receiptPhoto.CopyToAsync(stream);
            receiptPath = $"/uploads/receipts/{fileName}";
        }

        try
        {
            await _subService.SubmitRequestAsync(GetUserId(), receiptPath, requestedTier);
            TempData["SuccessMessage"] = "✅ Subscription request submitted! An admin will review it.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
}