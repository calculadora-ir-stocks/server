using FluentValidation.Results;

namespace Core.Notification
{
    public class NotificationManager
    {
        private readonly List<Notification> notifications;
        public IReadOnlyCollection<Notification> Notifications => notifications;
        public bool HasNotifications => notifications.Any();

        public NotificationManager()
        {
            notifications = new List<Notification>();
        }

        public void AddNotification(string message)
        {
            notifications.Add(new Notification(message));
        }

        public void AddNotifications(IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                notifications.Add(new Notification(message));
            }
        }

        public void AddNotifications(ValidationResult validationResult)
        {
            foreach (var error in validationResult.Errors)
            {
                AddNotification(error.ErrorMessage);
            }
        }
    }
}
