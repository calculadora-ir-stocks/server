using FluentValidation.Results;

namespace Api.Notification
{
    public class NotificationContext
    {
        private readonly List<Notification> _notifications;
        public IReadOnlyCollection<Notification> Notifications => _notifications;
        public bool HasNotifications => _notifications.Any();

        public NotificationContext()
        {
            _notifications = new List<Notification>();
        }

        public void AddNotification(string message)
        {
            _notifications.Add(new Notification(message));
        }

        public void AddNotifications(IEnumerable<string> messages)
        {
            foreach (var message in messages)
            {
                _notifications.Add(new Notification(message));
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
