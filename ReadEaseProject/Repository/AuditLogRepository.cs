using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;
    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task<List<AuditLog>> GetFilteredAsync(
        string? userId, string? role, string? action,
        DateTime? from, DateTime? to)
    {
        var q = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            q = q.Where(a => a.UserId.ToString() == userId);
        if (!string.IsNullOrWhiteSpace(role))
            q = q.Where(a => a.Role == role);
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action.Contains(action));
        if (from.HasValue)
            q = q.Where(a => a.CreatedAt >= from.Value.Date);
        if (to.HasValue)
            q = q.Where(a => a.CreatedAt < to.Value.Date.AddDays(1));

        return await q.OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync();
    }

    public async Task CreateAsync(AuditLog log)
    {
        await _db.AuditLogs.AddAsync(log);
        await _db.SaveChangesAsync();
    }
}