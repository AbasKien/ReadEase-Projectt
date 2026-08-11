using ReadEase.DTOs.Auditdtos;
using ReadEase.DTOs.Auth;
using ReadEase.DTOs.Authordots;
using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;
using ReadEase.DTOs.Notificationdtos;
using ReadEase.DTOs.Userdtos;
using ReadEase.Models;
using ReadEase.ViewModels.Auth;
using ReadEase.ViewModels.User;

namespace ReadEase.Helpers;

public static class ManualMapper
{
    // ── RegisterViewModel → User ─────────────────────────────────────────
    public static User ToUser(RegisterViewModel vm) => new()
    {
        Name = vm.Name,
        Email = vm.Email.ToLowerInvariant(),
    };

    // ── RegisterDto → User ───────────────────────────────────────────────
    public static User ToUser(RegisterDto dto) => new()
    {
        Name = dto.Name,
        Email = dto.Email.ToLowerInvariant(),
    };

    // ── User → UserViewModel ─────────────────────────────────────────────
    public static UserViewModel ToUserViewModel(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role.ToString(),
    };

    // ── User → AuthResponseDto ────────────────────────────────────────────
    public static AuthResponseDto ToAuthResponseDto(User u) => new()
    {
        Email = u.Email,
        Name = u.Name,
        Role = u.Role.ToString(),
        Token = string.Empty,   // caller sets Token + Expiration
        Expiration = default,
    };

    // ── User → UserProfileDto ─────────────────────────────────────────────
    public static UserProfileDto ToUserProfileDto(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role.ToString(),
    };

    // ── User → UserDashboardDto ───────────────────────────────────────────
    public static UserDashboardDto ToUserDashboardDto(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role.ToString(),
        BooksRead = 0,   // caller populates
        FavoritesCount = 0,   // caller populates
    };

    // ── User → AuthorDashboardDto ─────────────────────────────────────────
    public static AuthorDashboardDto ToAuthorDashboardDto(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        TotalBooks = 0,   // caller populates
        TotalReads = 0,   // caller populates
    };

    // ── Book → BookCardDto ────────────────────────────────────────────────
    public static BookCardDto ToBookCardDto(Book b, bool isFavorite = false) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        Category = b.Category,
        CoverImage = b.CoverImage,
        Description = b.Description,
        IsFree = b.IsFree,
        IsFavorite = isFavorite,
    };

    // ── Book → BookDetailDto ──────────────────────────────────────────────
    public static BookDetailDto ToBookDetailDto(
        Book b,
        bool isFavorite = false,
        double avgRating = 0,
        int ratingCount = 0,
        int userRating = 0,
        bool canRead = false) => new()
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Category = b.Category,
            CoverImage = b.CoverImage,
            Description = b.Description,
            IsFree = b.IsFree,
            PublishedAt = b.PublishedAt,
            IsFavorite = isFavorite,
            AverageRating = avgRating,
            RatingCount = ratingCount,
            UserRating = userRating,
            CanRead = canRead,
        };

    // ── Book → BookReaderDto ──────────────────────────────────────────────
    public static BookReaderDto ToBookReaderDto(
        Book b,
        int scrollPosition = 0,
        bool isFavorite = false) => new()
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Category = b.Category,
            Content = b.Content ?? string.Empty,
            Description = b.Description,
            CoverImage = b.CoverImage,
            IsFree = b.IsFree,
            PublishedAt = b.PublishedAt,
            ScrollPosition = scrollPosition,
            IsFavorite = isFavorite,
        };

    // ── BookReview → ReviewDto ────────────────────────────────────────────
    public static ReviewDto ToReviewDto(BookReview r) => new()
    {
        Id = r.Id,
        User = r.User?.Name ?? string.Empty,
        UserPhoto = r.User?.ProfilePhoto,
        Stars = r.Stars,
        Text = r.Text ?? string.Empty,
        Ts = new DateTimeOffset(
                        DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc),
                        TimeSpan.Zero)
                        .ToUnixTimeMilliseconds(),
        AuthorRepliedTs = r.AuthorRepliedAt.HasValue
                            ? new DateTimeOffset(
                                DateTime.SpecifyKind(r.AuthorRepliedAt.Value, DateTimeKind.Utc),
                                TimeSpan.Zero)
                                .ToUnixTimeMilliseconds()
                            : null,
    };

    // ── AuditLog → AuditLogPushDto ────────────────────────────────────────
    // AuditLog: Id, UserId, Role, Action, RecordAffected, IpAddress, Device, CreatedAt
    // AuditLogPushDto: Id, CreatedAt, UserId, UserName, Role, Action, RecordAffected, IpAddress, Device
    public static AuditLogPushDto ToAuditLogPushDto(AuditLog a, string? userName = null) => new()
    {
        Id = a.Id,
        CreatedAt = a.CreatedAt,
        UserId = a.UserId.HasValue ? a.UserId.ToString() : null,
        UserName = userName ?? string.Empty,
        Role = a.Role,
        Action = a.Action,
        RecordAffected = a.RecordAffected,
        IpAddress = a.IpAddress,
        Device = a.Device,
    };

    // ── Notification → NotificationDto ───────────────────────────────────
    // NotificationDto has: Id, Message, Link, IsRead, CreatedAt (no UserId)
    public static NotificationDto ToNotificationDto(Notification n) => new()
    {
        Id = n.Id,
        Message = n.Message,
        Link = n.Link,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
    };
}