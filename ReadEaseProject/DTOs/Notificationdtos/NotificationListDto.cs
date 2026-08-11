namespace ReadEase.DTOs.Notificationdtos
{
    public class NotificationListDto
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }

}
