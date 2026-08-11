using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Repository.Interfaces;
using ReadEase.Services;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Admin;
using ReadEase.ViewModels.Archive;
using ReadEase.ViewModels.AuditLog;
using ReadEase.ViewModels.Subscribe;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ISubscriptionService _subService;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IBookReadRepository _readRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public AdminController(
        IAdminService adminService, ISubscriptionService subService,
        IAuditLogRepository auditRepo, IBookRepository bookRepo,
        IBookReadRepository readRepo, IUserRepository userRepo,
        IAuditService audit)
    {
        _adminService = adminService; _subService = subService;
        _auditRepo = auditRepo; _bookRepo = bookRepo;
        _readRepo = readRepo; _userRepo = userRepo;
        _audit = audit;
    }

    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string AdminIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    private string AdminAgent => Request.Headers["User-Agent"].ToString();

    // ── DASHBOARD ─────────────────────────────────────────────────────────
    [HttpGet, Route("Admin"), Route("Admin/Index")]
    public async Task<IActionResult> Index()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return View("~/Views/Dashboard/AdminDashboard.cshtml", stats);
    }

    // ── PENDING PREMIUM BOOKS ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> PendingBooks()
    {
        var books = await _adminService.GetPendingBooksAsync();
        return View("~/Views/Admin/PendingBooks.cshtml", books);
    }


    [HttpGet]
    public async Task<IActionResult> PendingBookDetail(Guid id)
    {
        var books = await _adminService.GetPendingBooksAsync();
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book is null) return NotFound();
        return View("~/Views/Admin/PendingBookDetail.cshtml", book);
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingBooksCount()
    {
        var books = await _bookRepo.GetPendingAsync();
        return Json(new { count = books.Count });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePremiumBook(Guid bookId)
    {
        try
        {
            await _adminService.ApprovePremiumBookAsync(bookId, AdminId);
            await _audit.LogAsync(AdminId, "Admin", "ApprovePremiumBook",
                $"BookId:{bookId}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = "✅ Book approved and is now live.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(PendingBooks));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPremiumBook(RejectPremiumBookViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide a rejection reason.";
            return RedirectToAction(nameof(PendingBooks));
        }
        try
        {
            await _adminService.RejectPremiumBookAsync(vm.BookId, vm.Reason, AdminId);
            await _audit.LogAsync(AdminId, "Admin", "RejectPremiumBook",
                $"BookId:{vm.BookId}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = "Book rejected. Author has been notified.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(PendingBooks));
    }

    // ── USER MANAGEMENT ───────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _adminService.GetAllUsersAsync();
        // ✅ FILTER: exclude unconfirmed accounts
        var confirmedUsers = users.Where(u => u.IsEmailConfirmed).ToList();
        return View("~/Views/Admin/Usermanagement.cshtml", confirmedUsers);
    }

    // ✅ ADDED — Usermanagement.cshtml active user → deactivate
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user is null) { TempData["ErrorMessage"] = "User not found."; return RedirectToAction(nameof(Users)); }
            if (!user.IsActive) { TempData["ErrorMessage"] = "User is already inactive."; return RedirectToAction(nameof(Users)); }

            user.IsActive = false;
            await _userRepo.UpdateAsync(user);
            await _audit.LogAsync(AdminId, "Admin", "DeactivateUser",
                $"TargetUserId:{userId} Name:{user.Name}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = $"🚫 {user.Name} has been deactivated.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Users));
    }

    // ✅ ADDED — Usermanagement.cshtml inactive user → activate
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user is null) { TempData["ErrorMessage"] = "User not found."; return RedirectToAction(nameof(Users)); }
            if (user.IsActive) { TempData["ErrorMessage"] = "User is already active."; return RedirectToAction(nameof(Users)); }

            user.IsActive = true;
            await _userRepo.UpdateAsync(user);
            await _audit.LogAsync(AdminId, "Admin", "ActivateUser",
                $"TargetUserId:{userId} Name:{user.Name}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = $"✅ {user.Name} has been activated.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Users));
    }

    // Keep ToggleUser for any legacy callers
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(Guid userId)
    {
        try
        {
            await _adminService.ToggleUserActiveAsync(userId);
            await _audit.LogAsync(AdminId, "Admin", "ToggleUserActive",
                $"TargetUserId:{userId}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = "User status updated.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Users));
    }

    // ── SUBSCRIPTIONS ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Subscriptions()
    {
        var all = await _subService.GetAllRequestsAsync();
        var vms = all.Select(s => new SubscriptionAdminViewModel
        {
            Id = s.Id,
            UserName = s.User.Name,
            UserEmail = s.User.Email,
            ReceiptPhoto = s.ReceiptPhoto,
            Status = s.Status,
            Reason = s.Reason,
            SubmittedAt = s.SubmittedAt,
            ReviewedAt = s.ReviewedAt
        }).ToList();
        return View("~/Views/Admin/Subscriptions.cshtml", vms);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSubscription(Guid requestId)
    {
        try
        {
            await _subService.ApproveAsync(requestId);
            await _audit.LogAsync(AdminId, "Admin", "ApproveSubscription",
                $"RequestId:{requestId}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = "✅ Subscription approved.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Subscriptions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectSubscription(RejectSubscriptionViewModel vm)
    {
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Provide a reason."; return RedirectToAction(nameof(Subscriptions)); }
        try
        {
            await _subService.RejectAsync(vm.RequestId, vm.Reason);
            await _audit.LogAsync(AdminId, "Admin", "RejectSubscription",
                $"RequestId:{vm.RequestId}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = "Subscription rejected.";
        }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Subscriptions));
    }

    // ── BOOKS MANAGEMENT ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Books()
    {
        var books = await _bookRepo.GetAllActiveAsync();
        var vms = new List<BookManagementViewModel>();
        foreach (var b in books)
        {
            var reads = await _readRepo.GetTotalReadsByBookAsync(b.Id);
            vms.Add(new BookManagementViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                IsDeleted = b.IsDeleted,
                PublishedAt = b.PublishedAt,
                ReadCount = reads
            });
        }
        return View("~/Views/Admin/Books.cshtml", vms);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBook(Guid bookId)
    {
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is not null)
        {
            book.IsDeleted = true;
            await _bookRepo.UpdateAsync(book);
            await _audit.LogAsync(AdminId, "Admin", "DeleteBook",
                $"BookId:{bookId} Title:{book.Title}", AdminIp, AdminAgent);
        }
        TempData["SuccessMessage"] = "Book moved to archive.";
        return RedirectToAction(nameof(Books));
    }

    // ── AUDIT LOGS ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> AuditLogs(
        string? userId, string? role, string? actionFilter, DateTime? from, DateTime? to)
    {
        var logs = await _auditRepo.GetFilteredAsync(userId, role, actionFilter, from, to);
        var users = await _userRepo.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        var vm = new AuditLogFilterViewModel
        {
            UserId = userId,
            Role = role,
            Action = actionFilter,
            From = from,
            To = to,
            Logs = logs.Select(l => new AuditLogViewModel
            {
                Id = l.Id,
                UserId = l.UserId?.ToString(),
                UserName = l.UserId.HasValue && userMap.TryGetValue(l.UserId.Value, out var n) ? n : "System",
                Role = l.Role,
                Action = l.Action,
                RecordAffected = l.RecordAffected,
                IpAddress = l.IpAddress,
                Device = l.Device,
                CreatedAt = l.CreatedAt
            }).ToList()
        };
        return View("~/Views/Admin/AuditLogs.cshtml", vm);
    }

    // ── ARCHIVE ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Archive()
    {
        var deletedBooks = await _bookRepo.GetDeletedAsync();
        var inactiveUsers = (await _userRepo.GetAllAsync()).Where(u => !u.IsActive).ToList();
        var vm = new ArchiveViewModel
        {
            DeletedBooks = deletedBooks.Select(b => new BookArchiveItem
            { Id = b.Id, Title = b.Title, Author = b.Author, Category = b.Category, PublishedAt = b.PublishedAt }).ToList(),
            InactiveUsers = inactiveUsers.Select(u => new UserArchiveItem
            { Id = u.Id, Name = u.Name, Email = u.Email, Role = u.Role.ToString() }).ToList()
        };
        return View("~/Views/Admin/Archive.cshtml", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreBook(Guid bookId)
    {
        var book = await _bookRepo.GetDeletedBookByIdAsync(bookId);
        if (book is not null)
        {
            book.IsDeleted = false;
            await _bookRepo.UpdateAsync(book);
            await _audit.LogAsync(AdminId, "Admin", "RestoreBook",
                $"BookId:{bookId}", AdminIp, AdminAgent);
        }
        TempData["SuccessMessage"] = "Book restored.";
        return RedirectToAction(nameof(Archive));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDeleteBook(Guid bookId)
    {
        await _bookRepo.HardDeleteAsync(bookId);
        await _audit.LogAsync(AdminId, "Admin", "PermanentDeleteBook",
            $"BookId:{bookId}", AdminIp, AdminAgent);
        TempData["SuccessMessage"] = "Book permanently deleted.";
        return RedirectToAction(nameof(Archive));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreUser(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is not null)
        {
            user.IsActive = true;
            await _userRepo.UpdateAsync(user);
            await _audit.LogAsync(AdminId, "Admin", "RestoreUser",
                $"TargetUserId:{userId}", AdminIp, AdminAgent);
        }
        TempData["SuccessMessage"] = "User account restored.";
        return RedirectToAction(nameof(Archive));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDeleteUser(Guid userId)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user is null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Archive));
            }

            var userName = user.Name;
            await _userRepo.DeleteAsync(user);
            await _audit.LogAsync(AdminId, "Admin", "PermanentDeleteUser",
                $"TargetUserId:{userId} Name:{userName}", AdminIp, AdminAgent);
            TempData["SuccessMessage"] = $"🗑️ {userName} has been permanently deleted.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Archive));
    }
}