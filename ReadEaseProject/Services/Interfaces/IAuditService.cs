namespace ReadEase.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(Guid? userId, string role, string action,
                      string? recordAffected = null,
                      string? ipAddress = null, string? device = null);
    }
}
