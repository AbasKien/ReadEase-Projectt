using Microsoft.AspNetCore.SignalR;
using ReadEase.DTOs.Auditdtos;
using ReadEase.Hubs;
using ReadEase.Models;
using ReadEase.Repository;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;

namespace ReadEase.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;
    private readonly IHubContext<AuditHub> _hub;
    private readonly IUserRepository _userRepo;

    public AuditService(
        IAuditLogRepository repo,
        IHubContext<AuditHub> hub,
        IUserRepository userRepo)
    {
        _repo = repo;
        _hub = hub;
        _userRepo = userRepo;
    }

    public async Task LogAsync(
        Guid? userId,
        string role,
        string action,
        string? recordAffected = null,
        string? ipAddress = null,
        string? device = null)
    {
        try
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = role,
                Action = action,
                RecordAffected = recordAffected,
                IpAddress = ipAddress,
                Device = device,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(log);

            // ── Resolve user name for the real-time push ──────────────────
            string userName = "System";
            if (userId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(userId.Value);
                userName = user?.Name ?? "Unknown";
            }

            // ── Push to all connected admin dashboards (typed DTO) ────────
            await _hub.Clients.Group("admins").SendAsync("ReceiveAuditLog",
                new AuditLogPushDto
                {
                    Id = log.Id,
                    CreatedAt = log.CreatedAt,
                    UserId = log.UserId?.ToString(),
                    UserName = userName,
                    Role = log.Role,
                    Action = log.Action,
                    RecordAffected = log.RecordAffected,
                    IpAddress = log.IpAddress,
                    Device = log.Device
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLog ERROR] {ex.Message}");
        }
    }
}