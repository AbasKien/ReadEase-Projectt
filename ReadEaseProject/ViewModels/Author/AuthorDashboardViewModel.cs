namespace ReadEase.ViewModels.Author;

public class AuthorDashboardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public int TotalBooks { get; set; }
    public int TotalReads { get; set; }
    public int ArchivedCount { get; set; }
    public List<ManageBookItem> Books { get; set; } = new();  // unified with ManageBookItem


}