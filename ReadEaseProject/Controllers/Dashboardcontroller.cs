using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Repository;
using ReadEase.Repository.Interfaces;
using ReadEase.ViewModels.Author;
using ReadEase.ViewModels.Library;
using ReadEase.ViewModels.User;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserRepository _userRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IBookReadRepository _readRepo;
    private readonly IFavoriteRepository _favRepo;
    private readonly IAuthorApplicationRepository _appRepo;

    public DashboardController(
        IUserRepository userRepo, IBookRepository bookRepo,
        IBookReadRepository readRepo, IFavoriteRepository favRepo,
        IAuthorApplicationRepository appRepo)
    {
        _userRepo = userRepo; _bookRepo = bookRepo;
        _readRepo = readRepo; _favRepo = favRepo;
        _appRepo = appRepo;
    }

    // GET /Dashboard/UserDashboard
    [HttpGet]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UserDashboard()
    {
        var userId = GetUserId();
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Auth");

        var recentReads = await _readRepo.GetRecentByUserAsync(userId, 6);
        var favorites = await _favRepo.GetByUserAsync(userId);
        var hasPending = await _appRepo.HasPendingApplicationAsync(userId);
        var totalReads = await _readRepo.GetTotalReadsByUserAsync(userId);

        var vm = new UserDashboardViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Provider = user.Provider,
            ProfilePhoto = user.ProfilePhoto,
            IsSubscribed = user.IsSubscribed,
            HasPendingApplication = hasPending,
            IsEmailConfirmed = user.IsEmailConfirmed,
            BooksRead = totalReads,
            FavoritesCount = favorites.Count,
            RecentlyRead = recentReads.Select(r => new RecentBookItem
            {
                Id = r.Book.Id,
                Title = r.Book.Title,
                Author = r.Book.Author,
                Category = r.Book.Category,
                CoverImage = r.Book.CoverImage
            }).ToList(),
            Favorites = favorites.Take(6).Select(f => new RecentBookItem
            {
                Id = f.Book.Id,
                Title = f.Book.Title,
                Author = f.Book.Author,
                Category = f.Book.Category,
                CoverImage = f.Book.CoverImage
            }).ToList()
        };
        return View("~/Views/Dashboard/UserDashboard.cshtml", vm);
    }

    // GET /Dashboard/AuthorDashboard
    [HttpGet]
    [Authorize(Roles = "Author")]
    public async Task<IActionResult> AuthorDashboard()
    {
        var userId = GetUserId();
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Auth");

        var books = await _bookRepo.GetByAuthorUserIdAsync(userId);
        var bookIds = books.Select(b => b.Id).ToList();

        // Batch-fetch unique reader counts in one query
        var readCounts = await _readRepo.GetReadCountsBatchAsync(bookIds);

        var bookItems = books.Select(b => new ManageBookItem
        {
            Id = b.Id,
            Title = b.Title,
            Category = b.Category,
            CoverImage = b.CoverImage,
            IsFree = b.IsFree,
            IsDeleted = b.IsDeleted,
            PublishedAt = b.PublishedAt,
            ReadCount = readCounts.GetValueOrDefault(b.Id, 0)  // unique readers
        }).ToList();

        var vm = new AuthorDashboardViewModel
        {
            Name = user.Name,
            Email = user.Email,
            ProfilePhoto = user.ProfilePhoto,
            TotalBooks = bookItems.Count(b => !b.IsDeleted),
            TotalReads = bookItems.Where(b => !b.IsDeleted).Sum(b => b.ReadCount),
            ArchivedCount = bookItems.Count(b => b.IsDeleted),
            Books = bookItems
        };
        return View("~/Views/Dashboard/AuthorDashboard.cshtml", vm);
    }

    // GET /Dashboard/AdminDashboard  → redirect
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminDashboard()
        => RedirectToAction("Index", "Admin");

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
}