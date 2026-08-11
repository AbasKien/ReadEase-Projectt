using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetFilteredAsync(
        string? userId, string? role, string? action,
        DateTime? from, DateTime? to);
    Task CreateAsync(AuditLog log);
}