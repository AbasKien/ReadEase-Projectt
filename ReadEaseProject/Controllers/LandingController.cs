using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;

namespace ReadEase.Controllers;

public class LandingController : Controller
{
    private readonly ILandingService _landingService;

    public LandingController(ILandingService landingService)
    {
        _landingService = landingService;
    }

    [HttpGet]
    public async Task<IActionResult> Landing()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Library");

        var vm = await _landingService.GetLandingDataAsync();
        return View("~/Views/Auth/Landing.cshtml", vm);
    }

    // ── Guest book preview — no login required ────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GuestRead(Guid id)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Read", "Library", new { id });

        var vm = await _landingService.GetBookPreviewAsync(id);
        if (vm is null) return NotFound();

        return View("~/Views/Auth/Guestread.cshtml", vm);
    }
}