using Microsoft.EntityFrameworkCore;
using ReadEase.Enums;
using ReadEase.Models;

namespace ReadEase.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuthorApplication> AuthorApplications => Set<AuthorApplication>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookChapter> BookChapters => Set<BookChapter>();
    public DbSet<BookRead> BookReads => Set<BookRead>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<SubscriptionRequest> SubscriptionRequests => Set<SubscriptionRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BookRating> BookRatings => Set<BookRating>();
    public DbSet<BookReview> BookReviews => Set<BookReview>();
    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();
    public DbSet<ReviewCommentLike> ReviewCommentLikes => Set<ReviewCommentLike>();

    // ── NEW: Author Profile Feature ────────────────────────────────────────
    public DbSet<UserPost> UserPosts => Set<UserPost>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Name).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.Provider).HasMaxLength(50).HasDefaultValue("Local");
            e.Property(u => u.ExternalId).HasMaxLength(200);
            e.Property(u => u.ProfilePhoto).HasMaxLength(500);
            e.Property(u => u.CoverPhoto).HasMaxLength(500);

            // SubscriptionTier is an int column. No HasDefaultValue here —
            // the CLR default (0 = Free) is what EF will insert, which is correct.
            // Using HasDefaultValue() on an enum whose CLR default == DB default
            // confuses EF's sentinel detection and causes the warning.
            e.Property(u => u.SubscriptionTier)
             .HasConversion<int>()
             .IsRequired();

            // These are computed helpers — not mapped to any DB column
            e.Ignore(u => u.IsSubscribed);
            e.Ignore(u => u.IsPremiumPlus);
        });

        b.Entity<AuthorApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedOnAdd();
            e.Property(a => a.Bio).IsRequired().HasMaxLength(2000);
            e.Property(a => a.RejectionReason).HasMaxLength(500);
            e.HasOne(a => a.User).WithMany()
             .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Book>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Author).IsRequired().HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(500);
            e.Property(x => x.CoverImage).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(10000);
            e.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        });

        // ── BookChapter ────────────────────────────────────────────────────
        b.Entity<BookChapter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Title).IsRequired().HasMaxLength(300);
            e.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.BookId, x.ChapterNumber }).IsUnique();
            e.HasOne(x => x.Book).WithMany()
             .HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BookRead>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Bookmark>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SubscriptionRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.ReceiptPhoto).HasMaxLength(500);
            e.Property(x => x.Reason).HasMaxLength(500);

            // Same reasoning: no HasDefaultValue on RequestedTier.
            // The C# property already initialises to Premium (= 1), so EF will
            // always insert 1 unless your code explicitly sets something else.
            // Removing HasDefaultValue eliminates the sentinel warning entirely.
            e.Property(x => x.RequestedTier)
             .HasConversion<int>()
             .IsRequired();

            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Message).IsRequired().HasMaxLength(500);
            e.Property(x => x.Link).HasMaxLength(300);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Action).IsRequired().HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(50);
            e.Property(x => x.RecordAffected).HasMaxLength(300);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.Device).HasMaxLength(300);
        });

        b.Entity<BookRating>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.Property(x => x.Stars).IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BookReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.UserId, x.BookId }).IsUnique();
            e.Property(x => x.Text).HasColumnType("nvarchar(max)");
            e.Property(x => x.AuthorReply).HasColumnType("nvarchar(max)");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ReviewLike>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.UserId, x.ReviewId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Review).WithMany().HasForeignKey(x => x.ReviewId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ReviewComment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Text).IsRequired().HasColumnType("nvarchar(max)");
            e.Property(x => x.ReplyingTo).HasMaxLength(200);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Review).WithMany().HasForeignKey(x => x.ReviewId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ReviewCommentLike>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.UserId, x.CommentId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Comment).WithMany().HasForeignKey(x => x.CommentId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ════════════════════════════════════════════════════════════════
        //  NEW: Author Profile Feature
        // ════════════════════════════════════════════════════════════════

        // ── UserPost ───────────────────────────────────────────────────────
        b.Entity<UserPost>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Content).IsRequired().HasMaxLength(1000);

            // Author FK — Restrict to avoid multiple cascade path from User
            e.HasOne(x => x.Author)
             .WithMany()
             .HasForeignKey(x => x.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            // WallOwner FK — whose profile wall this post belongs to
            e.HasOne(x => x.WallOwner)
             .WithMany()
             .HasForeignKey(x => x.WallOwnerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PostComment ────────────────────────────────────────────────────
        b.Entity<PostComment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Content).IsRequired().HasMaxLength(500);
            e.Property(x => x.ReplyToName).HasMaxLength(100);

            // Post FK — cascade delete all comments when post is deleted
            e.HasOne(x => x.Post)
             .WithMany(p => p.Comments)
             .HasForeignKey(x => x.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            // Author FK — Restrict to avoid cycle
            e.HasOne(x => x.Author)
             .WithMany()
             .HasForeignKey(x => x.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing: reply → parent comment (NO cascade — avoids cycle)
            e.HasOne(x => x.ParentComment)
             .WithMany(x => x.Replies)
             .HasForeignKey(x => x.ParentCommentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PostLike ───────────────────────────────────────────────────────
        b.Entity<PostLike>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();

            // One like per user per post
            e.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();

            e.HasOne(x => x.Post)
             .WithMany(p => p.Likes)
             .HasForeignKey(x => x.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UserFollow ─────────────────────────────────────────────────────
        b.Entity<UserFollow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();

            // One follow per pair
            e.HasIndex(x => new { x.FollowerId, x.FollowedId }).IsUnique();

            // Both FKs Restrict — prevents multiple cascade paths from User
            e.HasOne(x => x.Follower)
             .WithMany()
             .HasForeignKey(x => x.FollowerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Followed)
             .WithMany()
             .HasForeignKey(x => x.FollowedId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}