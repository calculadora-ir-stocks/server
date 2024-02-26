using Common.Configurations;
using Core.Notification;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;

namespace Core.Filters
{
    public class NotificationFilter : IAsyncResultFilter
    {
        private readonly NotificationManager notificationManager;
        private readonly JsonSerializerConfiguration configuration;

        public NotificationFilter(NotificationManager notificationManager, JsonSerializerConfiguration configuration)
        {
            this.notificationManager = notificationManager;
            this.configuration = configuration;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (notificationManager.HasNotifications)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.HttpContext.Response.ContentType = "application/json";

                // notificationManager.Notifications é uma lista e, mesmo se houver apenas 1 item,
                // JsonConvert.SerializeObject vai serializar um JSON em formato de lista. Foi decidido (por mim, já que essa base é uma autocracia)
                // que o response será em formato de lista apenas caso tenha mais de 1 notificação.
                var notifications = JsonConvert.SerializeObject(
                    (notificationManager.GetNotifications.Count() == 1 ? notificationManager.GetNotifications.Select(x => x).First() : 
                    notificationManager.GetNotifications), configuration.Settings
                );

                await context.HttpContext.Response.WriteAsync(notifications);

                return;
            }

            await next();
        }
    }
}
